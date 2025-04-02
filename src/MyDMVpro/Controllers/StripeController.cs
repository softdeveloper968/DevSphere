using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Models;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using MyDMVpro.Models.DTOs;
using Microsoft.Extensions.DependencyInjection;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using UserInfo = MyDMVpro.Common.UserInfo;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;

namespace MyDMVpro.Controllers
{
    public class StripeController : BaseController
    {
        private readonly string _apiKey;
        private readonly IServiceProvider _serviceProvider;

        public StripeController(MaggardDMVContext context, IConfiguration configuration, ILogger<PaymentReportController> logger, IServiceProvider serviceProvider) : base(context, configuration, logger)
        {
            _apiKey = configuration["Stripe:SecretKey"];
            StripeConfiguration.ApiKey = _apiKey;
            _serviceProvider = serviceProvider;
        }

        public async Task<IActionResult> PaymentSuccess()
        {
            return View();
        }

        public async Task<IActionResult> PaymentFailed()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableCors("AllowStripe")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"];
            var secret = _configuration["Stripe:WebhookSecret"];

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, secret);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Webhook error: {ex.Message}");
                return BadRequest();
            }

            _ = Task.Run(async () =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<MaggardDMVContext>(); // Get new DB context
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<StripeController>>(); // Get Logger
                    await ProcessStripeEvent(stripeEvent, scopedContext, scopedLogger);
                    
                }
            });

            return Ok();
        }

        [HttpPost]
        public IActionResult CreateSession([FromBody] PaymentRequestDto request)
        {
            UserInfo user = GetCurrentUser();
            if (user == null) 
            {
                return JsonError("User not found. Please try by login again.");
            }

            if (request?.Amount <= 0)
            {
                return BadRequest("Invalid payment amount.");
            }

            string sessionUrl = CreatePaymentSession(request.Amount, request.ServiceCharge, request.Currency, request.SuccessUrl, request.CancelUrl, user.UserId, user.VendorId, request.RequestId, request.Vin, request.PaymentMethodType);

            return Ok(new { url = sessionUrl });
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentLink(Guid id)
        {
            try
            {
                UserInfo user = GetCurrentUser();
            
                if (user == null)
                {
                    return JsonError("User not found. Please try by login again.");
                }

                var existingPaymentLink = await _context.PaymentLinks
                             .Where(p => p.UserId == user.UserId
                             && p.RequestId == id
                             && p.ExpirationDate > DateTime.UtcNow
                             && !p.IsPaid).ToListAsync();

                return Ok(new { success = true, data = existingPaymentLink });

            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteLink(Guid id)
        {
            try
            {
                UserInfo user = GetCurrentUser();

                if (user == null)
                {
                    return JsonError("User not found. Please try by login again.");
                }
                var existingPaymentLink = await _context.PaymentLinks
                             .Where(p => p.Id == id).FirstOrDefaultAsync();

                _context.Remove(existingPaymentLink);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Payment link is successfully deleted" });

            }
            catch (Exception)
            {
                throw;
            }
        }
      
        [HttpPost]
        public IActionResult CreatePaymentLink([FromBody] PaymentRequestDto request)
        {
            UserInfo user = GetCurrentUser();
            if (user == null || user.UserId == null)
            {
                return JsonError("User not found. Please try by login again.");
            }

            if (request?.Amount <= 0)
            {
                return BadRequest("Invalid payment amount.");
            }
            var existingPaymentLink = _context.PaymentLinks
                .FirstOrDefault(p => p.UserId == user.UserId
                                    && p.Amount == request.Amount
                                    && p.ServiceCharge == request.ServiceCharge
                                    && p.Currency == request.Currency
                                    && p.ExpirationDate > DateTime.UtcNow
                                    && !p.IsPaid);

            if (existingPaymentLink != null)
            {
                return Ok(new { url = existingPaymentLink.PaymentLinkUrl, message = "Payment url is already existing for this request and amount. Please use this url." });
            }
            else
            {
                string sessionUrl = CreatePaymentLink(request.Amount, request.ServiceCharge, request.Currency, user.UserId, user.VendorId, request.RequestId, request.Vin, request.PaymentMethodType);

                return Ok(new { url = sessionUrl, message = "" });
            }
        }

        public string CreatePaymentSession(decimal amount, decimal serviceCharge, string currency, string successUrl, string cancelUrl, Guid? userId, Guid? vendorId, Guid RequestId, string Vin , string paymentMethodType)
        {

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { paymentMethodType },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = (long)(amount * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Invoice Payment"
                            }
                        },
                        Quantity = 1,
                    },
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = (long)(serviceCharge * 100), // Convert to cents
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Service Charge"
                            }
                        },
                        Quantity = 1,
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "UserId", userId.ToString() },
                        { "VendorId", vendorId.ToString() },
                        { "RequestId", RequestId.ToString() },
                        { "VIN", Vin },
                        { "PaymentLinkId", "" },
                        { "PaymentType", "Checkout" },
                        { "PaymentMethod", paymentMethodType },
                        { "Amount", amount.ToString() },
                        { "ServiceCharge", serviceCharge.ToString() },

                    }
                }
            };

            var service = new SessionService();
            var session = service.Create(options);

           

            return session.Url;

        }

        public string CreatePaymentLink(decimal amount, decimal serviceCharge, string currency, Guid? userId, Guid? vendorId, Guid requestId, string vin, string paymentMethodType)
        {
            var existingPaymentLink = _context.PaymentLinks
                .FirstOrDefault(p => p.UserId == userId
                                     && p.Amount == amount
                                     && p.ServiceCharge == serviceCharge
                                     && p.Currency == currency
                                     && p.ExpirationDate > DateTime.UtcNow
                                     && !p.IsPaid);

            if (existingPaymentLink != null)
            {
                return existingPaymentLink.PaymentLinkUrl;
            }

            var priceService = new PriceService();

            var invoicePrice = priceService.Create(new PriceCreateOptions
            {
                UnitAmount = (long)(amount * 100),
                Currency = currency,
                ProductData = new PriceProductDataOptions { Name = "Invoice Payment" }
            });

            var serviceChargePrice = priceService.Create(new PriceCreateOptions
            {
                UnitAmount = (long)(serviceCharge * 100),
                Currency = currency,
                ProductData = new PriceProductDataOptions { Name = "Service Charge" }
            });

            var options = new PaymentLinkCreateOptions
            {
                PaymentMethodTypes = new List<string> { paymentMethodType },
                LineItems = new List<PaymentLinkLineItemOptions>
                {
                    new PaymentLinkLineItemOptions { Price = invoicePrice.Id, Quantity = 1 },
                    new PaymentLinkLineItemOptions { Price = serviceChargePrice.Id, Quantity = 1 }
                },
                PaymentIntentData = new PaymentLinkPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "UserId", userId.ToString() },
                        { "VendorId", vendorId.ToString() },
                        { "RequestId", requestId.ToString() },
                        { "VIN", vin },
                        { "PaymentType", "Payment Link" },
                        { "PaymentMethod", paymentMethodType },
                        { "Amount", amount.ToString() },
                        { "ServiceCharge", serviceCharge.ToString() },
                    }

                },
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", userId.ToString() },
                    { "VendorId", vendorId.ToString() },
                    { "RequestId", requestId.ToString() },
                    { "VIN", vin },
                    { "PaymentType", "Payment Link" },
                    { "PaymentMethod", paymentMethodType },
                    { "Amount", amount.ToString() },
                    { "ServiceCharge", serviceCharge.ToString() },
                }
            };

            var paymentLinkService = new PaymentLinkService();
            var paymentLink = paymentLinkService.Create(options);

            var newPaymentLink = new Models.PaymentLink
            {
                UserId = (Guid)userId,
                VendorId = (Guid)vendorId,
                RequestId = (Guid)requestId,
                Amount = amount,
                ServiceCharge = serviceCharge,
                Currency = currency,
                PaymentLinkUrl = paymentLink.Url,
                ExpirationDate = DateTime.UtcNow.AddDays(7) ,
                PaymentMethod = paymentMethodType,
                IsPaid = false
            };

            _context.PaymentLinks.Add(newPaymentLink);
            _context.SaveChanges();

            var updateOptions = new PaymentLinkUpdateOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "PaymentLinkId", newPaymentLink.Id.ToString() }
                }
            };

            var updatedPaymentLink = paymentLinkService.Update(paymentLink.Id, updateOptions);
            return updatedPaymentLink.Url;
        }

        private async Task ProcessStripeEvent(Event stripeEvent, MaggardDMVContext context, ILogger logger)
        {
            try
            {
                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        await HandlePaymentIntentEvent(stripeEvent, context, logger);
                        break;
                    case "payment_intent.payment_failed":
                        await HandlePaymentIntentEvent(stripeEvent, context, logger);
                        break;

                    case "checkout.session.completed":
                        await HandleCheckoutSessionEvent(stripeEvent, context, logger);
                        break;

                    case "charge.succeeded":
                        await HandleChargeSucceededEvent(stripeEvent, context, logger);
                        break;

                    default:
                        logger.LogWarning($"Unhandled event type: {stripeEvent.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing webhook: {ex.Message}");
            }
        }

        private async Task HandlePaymentIntentEvent(Stripe.Event stripeEvent, MaggardDMVContext context, ILogger logger)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            var paymentIntentService = new PaymentIntentService();
            var retrievedIntent = await paymentIntentService.GetAsync(paymentIntent.Id);

            string userId = retrievedIntent.Metadata.TryGetValue("UserId", out var userIdStr) ? userIdStr : null;
            string paymentLinkId = retrievedIntent.Metadata.TryGetValue("PaymentLinkId", out var paymentLinkIdStr) ? paymentLinkIdStr : null;

            var existingPayment = await context.StripePayments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaymentIntentId == retrievedIntent.Id);

            if (existingPayment == null)
            {
                var payment = new StripePaymentModel
                {
                    PaymentId = Guid.NewGuid(),
                    UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : Guid.Empty,
                    VendorId = retrievedIntent.Metadata.TryGetValue("VendorId", out var vendorIdStr) ? Guid.Parse(vendorIdStr) : Guid.Empty,
                    RequestId = retrievedIntent.Metadata.TryGetValue("RequestId", out var reqId) ? Guid.Parse(reqId) : Guid.Empty,
                    VIN = retrievedIntent.Metadata.TryGetValue("VIN", out var vinValue) ? vinValue : "N/A",
                    PaymentMethod = retrievedIntent.Metadata.TryGetValue("PaymentMethod", out var paymentMethod) ? paymentMethod : "N/A",
                    PaymentType = retrievedIntent.Metadata.TryGetValue("PaymentType", out var paymentType) ? paymentType : "N/A",
                    Amount = retrievedIntent.Metadata.TryGetValue("Amount", out var amount) ? amount : "N/A",
                    TotalAmount = (paymentIntent.Amount / 100.0).ToString(),
                    ServiceCharge = retrievedIntent.Metadata.TryGetValue("ServiceCharge", out var serviceCharge) ? serviceCharge : "N/A",
                    Currency = retrievedIntent.Currency.ToUpper(),
                    PaymentIntentId = retrievedIntent.Id,
                    StripeTransactionId = retrievedIntent.LatestChargeId,
                    Status = retrievedIntent.Status,
                    CreatedAt = DateTime.UtcNow
                };

                context.StripePayments.Add(payment);
                await context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(paymentLinkId))
            {
                await MarkPaymentLinkAsPaid(paymentLinkId, userId, context);
            }
        }

        private async Task HandleCheckoutSessionEvent(Stripe.Event stripeEvent, MaggardDMVContext context, ILogger logger)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;
            string paymentIntentId = session.PaymentIntentId;
            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);
            string userId = paymentIntent.Metadata.TryGetValue("UserId", out var userIdStr) ? userIdStr : null;
            string paymentLinkId = paymentIntent.Metadata.TryGetValue("PaymentLinkId", out var paymentLinkIdStr) ? paymentLinkIdStr : null;

            var existingPayment = await context.StripePayments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId);

            if (existingPayment == null)
            {
                var payment = new StripePaymentModel
                {
                    PaymentId = Guid.NewGuid(),
                    UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : Guid.Empty,
                    VendorId = paymentIntent.Metadata.TryGetValue("VendorId", out var vendorIdStr) ? Guid.Parse(vendorIdStr) : Guid.Empty,
                    RequestId = paymentIntent.Metadata.TryGetValue("RequestId", out var reqId) ? Guid.Parse(reqId) : Guid.Empty,
                    VIN = paymentIntent.Metadata.TryGetValue("VIN", out var vinValue) ? vinValue : "N/A",
                    PaymentMethod = paymentIntent.Metadata.TryGetValue("PaymentMethod", out var paymentMethod) ? paymentMethod : "N/A",
                    PaymentType = paymentIntent.Metadata.TryGetValue("PaymentType", out var paymentType) ? paymentType : "N/A",
                    Amount = paymentIntent.Metadata.TryGetValue("Amount", out var amount) ? amount : "N/A",
                    TotalAmount = (paymentIntent.Amount / 100.0).ToString(),
                    ServiceCharge = paymentIntent.Metadata.TryGetValue("ServiceCharge", out var serviceCharge) ? serviceCharge : "N/A",
                    Currency = paymentIntent.Currency.ToUpper(),
                    PaymentIntentId = paymentIntent.Id,
                    StripeTransactionId = paymentIntent.LatestChargeId,
                    Status = paymentIntent.Status,
                    CreatedAt = DateTime.UtcNow
                };

                context.StripePayments.Add(payment);
                await context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(paymentLinkId))
            {
                await MarkPaymentLinkAsPaid(paymentLinkId, userId, context);
            }
        }

        private async Task HandleChargeSucceededEvent(Stripe.Event stripeEvent, MaggardDMVContext context, ILogger logger)
        {
            var charge = stripeEvent.Data.Object as Charge;
            if (charge == null) return;

            string userId = charge.Metadata.TryGetValue("UserId", out var userIdStr) ? userIdStr : null;
            string paymentLinkId = charge.Metadata.TryGetValue("PaymentLinkId", out var paymentLinkIdStr) ? paymentLinkIdStr : null;

            if (!string.IsNullOrEmpty(paymentLinkId))
            {
                await MarkPaymentLinkAsPaid(paymentLinkId, userId, context);
            }
        }

        private async Task MarkPaymentLinkAsPaid(string paymentLinkId, string userId, MaggardDMVContext context)
        {
            var paymentLink = await context.PaymentLinks
                .FirstOrDefaultAsync(p => p.UserId.ToString() == userId && !p.IsPaid && p.Id == Guid.Parse(paymentLinkId));

            if (paymentLink != null)
            {
                paymentLink.IsPaid = true;
                await context.SaveChangesAsync();
            }
        }

        
    }
}
