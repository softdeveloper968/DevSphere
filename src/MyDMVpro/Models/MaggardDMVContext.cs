using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Infrastructure; // for GetService<T>()
using MyDMVpro.Models.Tax;
using MyDMVpro.Controllers;
using System;
using System.Linq;
using System.Threading.Tasks;
using MyDMVpro.Models.DocumentsReceived_NoRequest;
using MyDMVpro.Models.ChatsViewModels;
using MyDMVpro.Models.RequestsViewModels;
using Microsoft.EntityFrameworkCore.Query;

namespace MyDMVpro.Models;

public partial class MaggardDMVContext : BaseMaggardDMVContext
{
    public MaggardDMVContext()
    {
    }

    public MaggardDMVContext(DbContextOptions<MaggardDMVContext> options)
        : base(options)
    {
    }
#if false
    public const int STATUS_ID_DELETED = 100;
    public new IQueryable<Requests> Requests
    {
        get
        {
            return base.Requests.Where(r => r.StatusId != STATUS_ID_DELETED);
        }
    }

    public async Task<int> SaveChangesAsync(Common.UserInfo user)
    {
        return await SaveChangesAsync(user?.UserId);
    }
    public async Task<int> SaveChangesAsync(Guid? userid = null)
    {
        // set user context
        this.Database.OpenConnection();
        await SetContextInfo(userid);
        return await base.SaveChangesAsync();
    }

    protected const string c_sqlSetUserContext = "exec dbo.SetUserContext @userid";

    protected async Task SetContextInfoAsync(Guid userid)
    {
        var useridParam = new Microsoft.Data.SqlClient.SqlParameter("@userid", System.Data.SqlDbType.UniqueIdentifier);
        useridParam.Value = userid;

        var data = await this.Database.ExecuteSqlRawAsync(c_sqlSetUserContext, useridParam);
    }
    protected Microsoft.Data.SqlClient.SqlParameter GetUserIdParam(Guid userid)
    {
        var useridParam = new Microsoft.Data.SqlClient.SqlParameter("@userid", System.Data.SqlDbType.UniqueIdentifier)
        {
            Value = userid
        };
        return useridParam;
    }
    protected async Task SetContextInfo(Guid? userid)
    {
        if (userid != null)
        {
            var data = await this.Database.ExecuteSqlRawAsync(c_sqlSetUserContext, GetUserIdParam(userid.Value));
        }
    }
#endif
}
public partial class BaseMaggardDMVContext : DbContext
{
    public BaseMaggardDMVContext()
    {
    }

    public BaseMaggardDMVContext(DbContextOptions<MaggardDMVContext> options)
        : base(options)
    {
    }
    public async Task<int> SaveChangesAsync(Common.UserInfo user)
    {
        return await SaveChangesAsync(user?.UserId);
    }
    public async Task<int> SaveChangesAsync(Guid? userid = null)
    {
        // set user context
        this.Database.OpenConnection();
        await SetContextInfo(userid);
        return await base.SaveChangesAsync();
    }
    protected const string c_sqlSetUserContext = "exec dbo.SetUserContext @userid";

    public async Task SetUserContext(Guid? userid)
    {
        if (userid != null)
        {
            var data = await this.Database.ExecuteSqlRawAsync(c_sqlSetUserContext, GetUserIdParam(userid.Value));
        }
    }
    protected async Task SetContextInfo(Guid? userid)
    {
        if (userid != null)
        {
            var data = await this.Database.ExecuteSqlRawAsync(c_sqlSetUserContext, GetUserIdParam(userid.Value));
        }
    }
    protected Microsoft.Data.SqlClient.SqlParameter GetUserIdParam(Guid userid)
    {
        var useridParam = new Microsoft.Data.SqlClient.SqlParameter("@userid", System.Data.SqlDbType.UniqueIdentifier)
        {
            Value = userid
        };
        return useridParam;
    }
    protected async Task SetContextInfoAsync(Guid userid)
    {
        var useridParam = new Microsoft.Data.SqlClient.SqlParameter("@userid", System.Data.SqlDbType.UniqueIdentifier);
        useridParam.Value = userid;

        var data = await this.Database.ExecuteSqlRawAsync(c_sqlSetUserContext, useridParam);
    }

    public const int STATUS_ID_DELETED = 100;
    public IQueryable<Requests> Requests
    {
        get
        {
            return RequestsAll.Where(r => r.StatusId != STATUS_ID_DELETED);
        }
    }

    public virtual DbSet<AppFormDefaults> AppFormDefaults { get; set; }
    public virtual DbSet<AttachmentReview> AttachmentReviews { get; set; }
    public virtual DbSet<DataReview> DataReviews { get; set; }
#if NO_LONGER_USED
    public virtual DbSet<AppFormProcessFields> AppFormProcessFields { get; set; }
    public virtual DbSet<AppFormSectionFields> AppFormSectionFields { get; set; }
    public virtual DbSet<AppFormSections> AppFormSections { get; set; }
    public virtual DbSet<ApplicationTypes> ApplicationTypes { get; set; }
    public virtual DbSet<AppTypeStates> AppTypeStates { get; set; }
    public virtual DbSet<AutoImsqueries> AutoImsqueries { get; set; }
#endif
    public virtual DbSet<Chats> Chats { get; set; }
    public virtual DbSet<Dmvoffice> Dmvoffice { get; set; }
    public virtual DbSet<FileUploads> FileUploads { get; set; }
    public virtual DbSet<FileData> FileData { get; set; }
    public virtual DbSet<FormTemplates> FormTemplates { get; set; }
    public virtual DbSet<GroupInvite> GroupInvite { get; set; }
    public virtual DbSet<Groups> Groups { get; set; }
    public virtual DbSet<GroupProfileType> GroupProfileType { get; set; }
    public virtual DbSet<GroupProfileTypeUsage> GroupProfileTypeUsage { get; set; }
    public virtual DbSet<GroupProfile> GroupProfiles { get; set; }
    public virtual DbSet<GroupProfileCategory> GroupProfileCategories { get; set; }
    public virtual DbSet<GroupProfileCategoryField> GroupProfileCategoryFields { get; set; }

    public virtual DbSet<GroupAttachment> GroupAttachments { get; set; }
    public virtual DbSet<GroupSettings> GroupSettings { get; set; }
    public virtual DbSet<GroupVendors> GroupVendors { get; set; }
    public virtual DbSet<ImportFields> ImportFields { get; set; }
    public virtual DbSet<ImportMapping> ImportMapping { get; set; }
    public virtual DbSet<Invoice> Invoice { get; set; }
    public virtual DbSet<InvoiceDetail> InvoiceDetail { get; set; }
    public virtual DbSet<MasterFields> MasterFields { get; set; }
    public virtual DbSet<MdpAppDefinitions> MdpAppDefinitions { get; set; }
    public virtual DbSet<MdpAppFields> MdpAppFields { get; set; }
    public virtual DbSet<MdpAppProcessFields> MdpAppProcessFields { get; set; }
    public virtual DbSet<MdpAppSectionFields> MdpAppSectionFields { get; set; }
    public virtual DbSet<MdpAppSections> MdpAppSections { get; set; }
    public virtual DbSet<MdpAppTypeCodes> MdpAppTypeCodes { get; set; }
    public virtual DbSet<MdpAppTypes> MdpAppTypes { get; set; }
    public virtual DbSet<MdpAppTypeStates> MdpAppTypeStates { get; set; }
    public virtual DbSet<MdpAttachmentTypes> MdpAttachmentTypes { get; set; }
    public virtual DbSet<MdpAttachmentTypeReviewColumn> MdpAttachmentTypeReviewColumns { get; set; }
    public virtual DbSet<MdpAppTypeAttachmentTypes> MdpAppTypeAttachmentTypes { get; set; }
    public virtual DbSet<MdpAppTypeAttachmentTypeReviewColumn> MdpAppTypeAttachmentTypeReviewColumns { get; set; }

    public virtual DbSet<MdpFieldTypes> MdpFieldTypes { get; set; }
    public virtual DbSet<Organization> Organizations { get; set; }
    public virtual DbSet<OrganizationContact> OrganizationContacts { get; set; }

    public virtual DbSet<PendingSignBatch> PendingSignBatch { get; set; }
    public virtual DbSet<PendingSignRequest> PendingSignRequest { get; set; }
    public virtual DbSet<PoliceAgency> PoliceAgency { get; set; }
    public virtual DbSet<PoliceDept> PoliceDept { get; set; }
    public virtual DbSet<PreferredVendor> PreferredVendor { get; set; }
    public virtual DbSet<ProcessStages> ProcessStages { get; set; }
    public virtual DbSet<RequestAttachments> RequestAttachments { get; set; }
    public virtual DbSet<RequestDisbursement> RequestDisbursements { get; set; }
    public virtual DbSet<RequestAttachmentReviewHistory> RequestAttachmentReviewHistories { get; set; }

    public virtual DbSet<PdfTemplate> PdfTemplates { get; set; }
    public virtual DbSet<RequestNotes> RequestNotes { get; set; }
    public virtual DbSet<RequestPdf> RequestPdf { get; set; }
    public virtual DbSet<Requests> RequestsAll { get; set; }
    public virtual DbSet<RequestSigning> RequestSigning { get; set; }
    public virtual DbSet<RequestTracking> RequestTracking { get; set; }
    public virtual DbSet<Signings> Signings { get; set; }
    public virtual DbSet<Status> Status { get; set; }
    public virtual DbSet<SysAdmins> SysAdmins { get; set; }
    public virtual DbSet<Uicolumns> Uicolumns { get; set; }
    public virtual DbSet<UiviewColumns> UiviewColumns { get; set; }
    public virtual DbSet<Uiviews> Uiviews { get; set; }
    public virtual DbSet<UserGroups> UserGroups { get; set; }
    public virtual DbSet<Users> Users { get; set; }
    public virtual DbSet<AuditResponse> AuditResponse { get; set; }
    public virtual DbSet<AuditFollowUpResponse> AuditFollowUpResponse { get; set; }
    public virtual DbSet<AuditCompleteResponse> AuditCompleteResponse { get; set; }
    public virtual DbSet<AuditBatch> AuditBatch { get; set; }
    public virtual DbSet<AuditOutcomeComboboxData> AuditOutcomeComboboxData { get; set; }
    public virtual DbSet<UploadedOCRDocuments> UploadedOCRDocuments { get; set; }
    public virtual DbSet<UserSettings> UserSettings { get; set; }
    public virtual DbSet<UserViews> UserViews { get; set; }
    public virtual DbSet<VendorAgent> VendorAgent { get; set; }
    public virtual DbSet<VendorFormFill> VendorFormFill { get; set; }
    public virtual DbSet<VendorInvite> VendorInvite { get; set; }
    public virtual DbSet<Vendors> Vendors { get; set; }
    public virtual DbSet<VendorAttachment> VendorAttachments { get; set; }
    public virtual DbSet<VendorSettings> VendorSettings { get; set; }
    public virtual DbSet<VendorState> VendorState { get; set; }
    public virtual DbSet<ViewDefinitions> ViewDefinitions { get; set; }
    public virtual DbSet<Vindetail> Vindetail { get; set; }
    public virtual DbSet<VinPartialDetail> VinPartialDetail { get; set; }
    public virtual DbSet<ZipCodes> ZipCodes { get; set; }
    public virtual DbSet<MdpAttachmentTypes> AttachmentTypes { get; set; }
    public virtual DbSet<MdpAppTypeAttachmentTypes> AppTypeAttachmentTypes { get; set; }
    public virtual DbSet<BulkPrintFile> BulkPrintFiles { get; set; }
    public virtual DbSet<BulkPrintFile> BulkApplicationFiles { get; set; }
    public virtual DbSet<Shipment> Shipments { get; set; }
    public virtual DbSet<ShipmentDetail> ShipmentDetails { get; set; }
    public virtual DbSet<RequestStatus> RequestStatus { get; set; }
    public virtual DbSet<RequestAttachmentCondition> RequestAttachmentConditions { get; set; }
    public virtual DbSet<InvoiceStatus> InvoiceStatus { get; set; }
    public virtual DbSet<FormAnalyzerForms> FormAnalyzerForms { get; set; }
    public virtual DbSet<FormAnalyzerUploads> FormAnalyzerUploads { get; set; }
    public virtual DbSet<RequestChats> RequestChats { get; set; }
    public virtual DbSet<ChatStatus> ChatStatus { get; set; }
    public virtual DbSet<ChatMessages> UnreadChatMessages { get; set; }
    public virtual DbSet<CommunicationChatMessages> UnreadCommunicationChatMessages { get; set; }
    public virtual DbSet<RequestFollowUps> RequestFollowUps { get; set; }
    public virtual DbSet<RequestFollowUpHistory> RequestFollowUpHistory { get; set; }
    public virtual DbSet<RequestFollowUpStatus> RequestFollowUpStatus { get; set; }
    public virtual DbSet<RequestCode> RequestCodes { get; set; }
    public virtual DbSet<RequestCodeFields> RequestCodeFields { get; set; }
    public virtual DbSet<FollowUpTags> FollowUpTags { get; set; }
    public virtual DbSet<FollowUpContacts> FollowUpContacts { get; set; }

    public virtual DbSet<Tag> Tag { get; set; }
    public virtual DbSet<TagCategory> TagCategories { get; set; }
    public virtual DbSet<GroupProfile> GroupProfile { get; set; }
    public virtual DbSet<NeedToProcessResponse> NeedToProcessResponses { get; set; }
    public virtual DbSet<ProcessingDayEnum> ProcessingDayEnum { get; set; }
    public virtual DbSet<ProposedUpdates> ProposedUpdates { get; set; }
    public virtual DbSet<UploadLinks> UploadLinks { get; set; }
    public virtual DbSet<NotifyClient> NotifyClient { get; set; }
    public virtual DbSet<US_State> States { get; set; }
    public virtual DbSet<CalculationTables> CalculationTables { get; set; }
    public virtual DbSet<TaxableItem> TaxableItems { get; set; }
    public virtual DbSet<TaxRule> TaxRules { get; set; }

    //public virtual DbSet<TaxCalculationRecord> TaxCalculationRecords { get; set; }
    public virtual DbSet<Jurisdiction> Jurisdictions { get; set; }
    public virtual DbSet<TaxFormula> TaxFormulas { get; set; }
    public virtual DbSet<PaymentsAndDisbursement> PaymentsAndDisbursement { get; set; }
    public virtual DbSet<PaymentTypes> PaymentTypes { get; set; }
    public virtual DbSet<VendorGroupPaymentsAndDisbursements> VendorGroupPaymentsAndDisbursements { get; set; }
    public virtual DbSet<PaymentsAndDisbursementHistory> PaymentsAndDisbursementHistory { get; set; }
    public virtual DbSet<DocumentReceived> DocumentsReceived { get; set; }
    public virtual DbSet<DocumentReceivedViewModel> DocumentsReceivedNoRequest { get; set; }
    public virtual DbSet<NeedMissingDataModel> NeedMissingDataModel { get; set; }
    public virtual DbSet<StateConfiguration> StateConfigurations { get; set; }
    public virtual DbSet<StripePaymentModel> StripePayments { get; set; }
    public virtual DbSet<PaymentLink> PaymentLinks { get; set; }
    public virtual DbSet<NotifyClientViewModal> NotifyClientView { get; set; }
    public virtual DbSet<FeaturePermission> FeaturePermissions { get; set; }
    public virtual DbSet<Feature> Features { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(MyDMVpro.Common.DataHelpers.SqlConnectionString);
#if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
#endif
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
   #if false
                    modelBuilder.Entity<AppFormDefaults>(entity =>
                    {
                        entity.HasIndex(e => new { e.AppType, e.AppTypeState, e.ExcelName })
                            .HasDatabaseName("IX_AppFormDefaults");

                        entity.Property(e => e.Id).HasColumnName("id");

                        entity.Property(e => e.AppType)
                            .IsRequired()
                            .HasMaxLength(5);

                        entity.Property(e => e.AppTypeState).HasMaxLength(2);

                        entity.Property(e => e.DefaultValue).HasMaxLength(100);

                        entity.Property(e => e.ExcelName).HasMaxLength(100);
                    });

                    modelBuilder.Entity<AppFormFields>(entity =>
                    {
                        entity.HasKey(e => e.FieldId);

                        entity.Property(e => e.FieldId).HasColumnName("fieldId");

                        entity.Property(e => e.ExcelName).HasMaxLength(100);

                        entity.Property(e => e.FieldDesc).HasMaxLength(100);

                        entity.Property(e => e.FieldHelpLink).HasMaxLength(200);

                        entity.Property(e => e.FieldLabel).HasMaxLength(100);

                        entity.Property(e => e.FieldPopupText).HasMaxLength(100);

                        entity.Property(e => e.FormId)
                            .IsRequired()
                            .HasMaxLength(100);

                        entity.Property(e => e.RegExValMsg).HasMaxLength(200);

                        entity.Property(e => e.RegExValidator).HasMaxLength(200);
                    });

                    modelBuilder.Entity<AppFormProcessFields>(entity =>
                    {
                        entity.HasKey(e => new { e.AppFormType, e.AppTypeState, e.ProcessFieldId });

                        entity.Property(e => e.AppFormType).HasMaxLength(5);

                        entity.Property(e => e.AppTypeState).HasMaxLength(2);

                        entity.HasOne(d => d.ProcessField)
                            .WithMany(p => p.AppFormProcessFields)
                            .HasForeignKey(d => d.ProcessFieldId)
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("FK_AppFormProcessFields_ProcessFields");
                    });

                    modelBuilder.Entity<AppFormSectionFields>(entity =>
                    {
                        entity.HasKey(e => new { e.SectionId, e.FieldId });

                        entity.Property(e => e.FieldLabel).HasMaxLength(100);

                        entity.HasOne(d => d.Field)
                            .WithMany(p => p.AppFormSectionFields)
                            .HasForeignKey(d => d.FieldId)
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("FK_AppFormSectionFields_AppFormFields");

                        entity.HasOne(d => d.Section)
                            .WithMany(p => p.AppFormSectionFields)
                            .HasForeignKey(d => d.SectionId)
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("FK_AppFormSectionFields_AppFormSections");
                    });

                    modelBuilder.Entity<AppFormSections>(entity =>
                    {
                        entity.HasKey(e => e.SectionId);

                        entity.Property(e => e.SectionId).HasColumnName("sectionId");

                        entity.Property(e => e.AppFormType)
                            .IsRequired()
                            .HasMaxLength(5);

                        entity.Property(e => e.SectionTitle)
                            .IsRequired()
                            .HasMaxLength(100);

                        entity.HasOne(d => d.AppFormTypeNavigation)
                            .WithMany(p => p.AppFormSections)
                            .HasForeignKey(d => d.AppFormType)
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("FK_AppFormSections_ApplicationTypes");
                    });

                    modelBuilder.Entity<ApplicationTypes>(entity =>
                    {
                        entity.HasKey(e => e.AppType);

                        entity.Property(e => e.AppType)
                            .HasMaxLength(5)
                            .ValueGeneratedNever();

                        entity.Property(e => e.AliasForAppType).HasMaxLength(5);

                        entity.Property(e => e.AutoIMSEnabled).HasColumnName("AutoIMSEnabled");

                        entity.Property(e => e.Description).HasMaxLength(100);

                        entity.Property(e => e.QueueName).HasMaxLength(10);
                    });

                    modelBuilder.Entity<AppTypeStates>(entity =>
                    {
                        entity.HasKey(e => new { e.AppType, e.AppTypeState });

                        entity.Property(e => e.AppType).HasMaxLength(5);

                        entity.Property(e => e.AppTypeState).HasMaxLength(2);

                        entity.Property(e => e.VendorCode).HasMaxLength(10);
                    });
                    modelBuilder.Entity<AutoImsqueries>(entity =>
                    {
                        entity.ToTable("AutoIMSQueries");

                        entity.HasIndex(e => e.UpdatedRequestId)
                            .HasDatabaseName("IX_AutoIMSQueries_RequestId");

                        entity.HasIndex(e => new { e.Vin, e.RunDate })
                            .HasDatabaseName("IX_AutoIMSQueries_Vin_RunDate");

                        entity.Property(e => e.Id).HasColumnName("id");

                        entity.Property(e => e.StatusCode).HasMaxLength(10);

                        entity.Property(e => e.Vin)
                            .IsRequired()
                            .HasColumnName("VIN")
                            .HasMaxLength(17);
                    });
            #endif
        modelBuilder.Entity<Chats>(entity =>
        {
            entity.HasKey(e => e.ChatId)
                  .IsClustered();

            entity.HasIndex(e => e.ChatId)
                .HasDatabaseName("CIX_Chats_ChatId")
                .IsUnique()
                .IsClustered();

            entity.HasIndex(e => new { e.RequestId, e.Created })
                .HasDatabaseName("IX_Chats_RequestId_Created");

            entity.Property(e => e.ChatId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.Created)
                .HasColumnName("created")
                .HasDefaultValueSql("(sysdatetime())");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Modified).HasColumnName("modified");

            entity.HasOne(d => d.Request)
                .WithMany(p => p.Chats)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chats_Requests");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Chats)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chats_Users");
        });

        modelBuilder.Entity<Dmvoffice>(entity =>
        {
            entity.HasKey(e => e.CommissionerId);

            entity.ToTable("DMVOffice");

            entity.Property(e => e.CommissionerId).HasColumnName("Commissioner_ID");

            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.City)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.Commissioner)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.NameAndAddress)
                .IsRequired()
                .HasMaxLength(214)
                .HasComputedColumnSql("(concat(rtrim([Commissioner]),N', ',rtrim([Address]),', ',rtrim([City]),', ',rtrim([State]),' ',[Zip]))");

            entity.Property(e => e.State)
                .IsRequired()
                .HasMaxLength(2)
                .IsUnicode(false);

            entity.Property(e => e.Zip)
                .IsRequired()
                .HasMaxLength(5)
                .IsUnicode(false);
        });

        modelBuilder.Entity<FileData>(entity =>
        {
            entity.HasKey(e => e.Id)
                .IsClustered(true);
        });

        modelBuilder.Entity<FileUploads>(entity =>
        {
            entity.HasKey(e => e.FileUploadId)
                .IsClustered(false);

            entity.HasIndex(e => e.Id)
                .HasDatabaseName("CIX_FileUploads_id")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.FileUploadId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.DateUploaded).HasDefaultValueSql("(sysdatetime())");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.ReferenceNo).HasMaxLength(20);

            entity.HasOne(d => d.User)
                .WithMany(p => p.FileUploads)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FileUploads_Users");
        });

        modelBuilder.Entity<FormTemplates>(entity =>
        {
            entity.HasIndex(e => new { e.VendorId, e.FormCode })
                .HasDatabaseName("IX_FormTemplates")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.AppType).HasMaxLength(5);

            entity.Property(e => e.FormCode).HasMaxLength(20);

            entity.Property(e => e.State)
                .IsRequired()
                .HasMaxLength(2);

            entity.HasOne(d => d.Vendor)
                .WithMany(p => p.FormTemplates)
                .HasForeignKey(d => d.VendorId)
                .HasConstraintName("FK_FormTemplates_Vendors");
        });

        modelBuilder.Entity<GroupInvite>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.Property(e => e.Accepted).HasDefaultValueSql("((0))");

            entity.Property(e => e.DateCreated).HasDefaultValueSql("(sysdatetime())");

            entity.Property(e => e.UserEmail)
                .IsRequired()
                .HasMaxLength(100);
        });

        modelBuilder.Entity<Groups>(entity =>
        {
            entity.HasKey(e => e.GroupId);

            entity.Property(e => e.GroupId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.Active)
                .IsRequired()
                .HasDefaultValueSql("((1))");

            entity.Property(e => e.AutoIMSEnabled).HasColumnName("AutoIMSEnabled");

            entity.Property(e => e.GroupName)
                .IsRequired()
                .HasMaxLength(100);
        });

        modelBuilder.Entity<GroupAttachment>(entity =>
        {
            entity.HasKey(e => new { e.GroupAttachmentId });

            entity.Property(e => e.LastModified)
                .HasDefaultValueSql("(sysdatetime())");

            entity.Property(e => e.ModifiedBy)
                .HasDefaultValueSql("(([dbo].[GetUserContext]()))");

            entity.HasIndex(e => new { e.GroupId, e.DisplayName })
                .HasDatabaseName("IX_GroupAttachment_GroupIdDisplayName")
                .IsUnique();
        });

        modelBuilder.Entity<VendorAttachment>(entity =>
        {
            entity.HasKey(e => new { e.VendorAttachmentId });

            entity.Property(e => e.LastModified)
                .HasDefaultValueSql("(sysdatetime())");

            entity.Property(e => e.ModifiedBy)
                .HasDefaultValueSql("(([dbo].[GetUserContext]()))");

            entity.HasIndex(e => new { e.VendorId, e.DisplayName })
                .HasDatabaseName("IX_VendorAttachment_VendorIdDisplayName")
                .IsUnique();
        });

        modelBuilder.Entity<GroupSettings>(entity =>
        {
            entity.HasKey(e => new { e.VendorId, e.GroupId, e.SettingsName });

            entity.Property(e => e.SettingsName).HasMaxLength(50);

            entity.Property(e => e.JSettings).HasColumnName("jSettings");
        });

        modelBuilder.Entity<GroupVendors>(entity =>
        {
            entity.HasKey(e => new { e.GroupId, e.VendorId });

            entity.HasOne(d => d.Group)
                .WithMany(p => p.GroupVendors)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupVendors_Groups");

            entity.HasOne(d => d.Vendor)
                .WithMany(p => p.GroupVendors)
                .HasForeignKey(d => d.VendorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupVendors_Vendors");
        });

        modelBuilder.Entity<ImportFields>(entity =>
        {
            entity.HasIndex(e => e.InternalName)
                .HasDatabaseName("IX_ImportFields")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.DataType).HasMaxLength(50);

            entity.Property(e => e.InternalName)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<ImportMapping>(entity =>
        {
            entity.HasKey(e => e.MappingId);

            entity.Property(e => e.MappingId)
                .HasColumnName("mappingId")
                .ValueGeneratedNever();

            entity.Property(e => e.JMapping).HasColumnName("jMapping");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id)
                .IsClustered(false);

            entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");

            entity.Property(e => e.SecondaryInvoice).HasDefaultValueSql("((0))");
        });

        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.HasKey(e => e.Id)
                .IsClustered(false);

            entity.Property(e => e.RequestNo).HasComputedColumnSql("([dbo].[fn_RequestNo]([RequestId]))", false);

            entity.Property(e => e.SecondaryInvoice).HasDefaultValueSql("((0))");

            entity.HasOne(d => d.Invoice)
                .WithMany(p => p.InvoiceDetails)
                .HasPrincipalKey(p => p.InvoiceId)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceDetail_Invoice");
        });
       
        //modelBuilder.Entity<Invoice>(entity =>
        //{
        //    entity.HasKey(e => e.Id)
        //        .IsClustered(false);

        //    entity.HasAlternateKey(e => e.InvoiceId);

        //    entity.HasIndex(e => new { e.VendorId, e.InvoiceNo })
        //        .HasDatabaseName("UK_Invoice_VendorId_InvoiceNo")
        //        .IsUnique();

        //    entity.Property(e => e.Id).HasColumnName("id");

        //    entity.Property(e => e.CustAddr1).HasMaxLength(100);

        //    entity.Property(e => e.CustAddr2).HasMaxLength(100);

        //    entity.Property(e => e.CustAttn).HasMaxLength(50);

        //    entity.Property(e => e.CustCity).HasMaxLength(50);

        //    entity.Property(e => e.CustName).HasMaxLength(50);

        //    entity.Property(e => e.CustState).HasMaxLength(2);

        //    entity.Property(e => e.CustZip).HasMaxLength(10);

        //    entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");

        //    entity.Property(e => e.DmvFees).HasColumnType("decimal(10, 2)");

        //    entity.Property(e => e.InvoiceAmount).HasColumnType("decimal(10, 2)");

        //    entity.Property(e => e.InvoiceNo)
        //        .IsRequired()
        //        .HasMaxLength(50);

        //    entity.Property(e => e.InvoiceNote).HasMaxLength(1024);

        //    entity.Property(e => e.OtherFees).HasColumnType("decimal(10, 2)");

        //    entity.Property(e => e.ServiceFees).HasColumnType("decimal(10, 2)");
        //});

        //modelBuilder.Entity<InvoiceDetail>(entity =>
        //{
        //    entity.HasKey(e => e.Id)
        //        .IsClustered(false);

        //    entity.HasIndex(e => new { e.RequestId, e.InvoiceListOrder })
        //        .HasDatabaseName("UK_InvoiceDetail_RequestId")
        //        .IsUnique();

        //    entity.HasOne(e => e.Invoice)
        //        .WithMany(e => e.InvoiceDetails)
        //        .HasForeignKey(e => e.InvoiceId);

        //    entity.Property(e => e.Id).HasColumnName("id");

        //    entity.Property(e => e.AbstractFee).HasColumnType("decimal(8, 2)");

        //    entity.Property(e => e.ClientRefNo).HasMaxLength(50);

        //    entity.Property(e => e.DateShipped).HasColumnType("date");

        //    entity.Property(e => e.DateSubmitted).HasColumnType("date");

        //    entity.Property(e => e.DmvFee)
        //        .HasColumnName("DMVDisbursement")
        //        .HasColumnType("decimal(8, 2)");

        //    entity.Property(e => e.MailingFee).HasColumnType("decimal(8, 2)");

        //    entity.Property(e => e.OtherDesc).HasMaxLength(50);

        //    entity.Property(e => e.OtherFee).HasColumnType("decimal(8, 2)");

        //    entity.Property(e => e.ServiceFee).HasColumnType("decimal(8, 2)");

        //    entity.Property(e => e.State).HasMaxLength(2);

        //    entity.Property(e => e.SvcCode).HasMaxLength(5);

        //    entity.Property(e => e.TotalDue).HasColumnType("decimal(8, 2)");

        //    entity.Property(e => e.GlCode).HasMaxLength(50);

        //    entity.Property(e => e.VIN).HasMaxLength(17);

        //    entity.Property(e => e.RequestNo).HasComputedColumnSql("([dbo].[fn_RequestNo]([RequestId]))");
        //});

        modelBuilder.Entity<MasterFields>(entity =>
        {
            entity.HasKey(e => e.FieldId);

            entity.HasIndex(e => new { e.VendorId, e.ExcelName })
                .HasDatabaseName("IX_MasterFields")
                .IsUnique();

            entity.HasIndex(e => new { e.VendorId, e.InternalName })
                .HasDatabaseName("IX_MasterFields_VendorId_InternalName")
                .IsUnique();

            entity.Property(e => e.FieldId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.AltExportSource).HasMaxLength(100);

            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(123)
                .HasComputedColumnSql("(concat([InternalName],' / ',[ExcelName]))");

            entity.Property(e => e.ExcelName).HasMaxLength(100);

            entity.Property(e => e.FormDefaultAllowDefault).HasColumnName("Form_Default_AllowDefault");

            entity.Property(e => e.FormDefaultDesc).HasColumnName("Form_Default_Desc");

            entity.Property(e => e.FormDefaultFieldTypeId).HasColumnName("Form_Default_FieldTypeId");

            entity.Property(e => e.FormDefaultHelpLink)
                .HasColumnName("Form_Default_HelpLink")
                .HasMaxLength(255);

            entity.Property(e => e.FormDefaultIsPii).HasColumnName("Form_Default_IsPII");

            entity.Property(e => e.FormDefaultIsRequired).HasColumnName("Form_Default_IsRequired");

            entity.Property(e => e.FormDefaultIsVisible).HasColumnName("Form_Default_IsVisible");

            entity.Property(e => e.FormDefaultLabel)
                .HasColumnName("Form_Default_Label")
                .HasMaxLength(100);

            entity.Property(e => e.FormDefaultPopupText)
                .HasColumnName("Form_Default_PopupText")
                .HasMaxLength(500);

            entity.Property(e => e.FormDefaultRegExValMsg)
                .HasColumnName("Form_Default_RegExValMsg")
                .HasMaxLength(200);

            entity.Property(e => e.FormDefaultRegExValidator)
                .HasColumnName("Form_Default_RegExValidator")
                .HasMaxLength(200);

            entity.Property(e => e.FormDefaultVendorIsRequired).HasColumnName("Form_Default_VendorIsRequired");

            entity.Property(e => e.FormDefaultVendorOnlyEdit).HasColumnName("Form_Default_VendorOnlyEdit");

            entity.Property(e => e.FormDefaultVendorOnlyVisible).HasColumnName("Form_Default_VendorOnlyVisible");

            entity.Property(e => e.FormDefaultVisibleOnEdit).HasColumnName("Form_Default_VisibleOnEdit");

            entity.Property(e => e.InternalName)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasOne(d => d.FormDefaultFieldType)
                .WithMany(p => p.MasterFields)
                .HasForeignKey(d => d.FormDefaultFieldTypeId)
                .HasConstraintName("FK_MasterFields_mdpFieldTypes");

            entity.HasOne(d => d.Vendor)
                .WithMany(p => p.MasterFields)
                .HasForeignKey(d => d.VendorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MasterFields_Vendors");

        });

        modelBuilder.Entity<MdpAppDefinitions>(entity =>
        {
            entity.HasKey(e => e.ApplicationId);

            entity.ToTable("mdpAppDefinitions");

            entity.Property(e => e.ApplicationId).HasDefaultValueSql("(newid())");
        });

        modelBuilder.Entity<MdpAppFields>(entity =>
        {
            entity.HasKey(e => e.FieldId);

            entity.ToTable("mdpAppFields");

            entity.HasIndex(e => e.VendorId)
                .HasDatabaseName("IX_AppFieldDefinitions_VendorId");

            entity.Property(e => e.FieldId)
                .HasColumnName("fieldId")
                .ValueGeneratedNever();

            entity.Property(e => e.ExcelName).HasMaxLength(100);

            entity.Property(e => e.FieldDesc).HasMaxLength(100);

            entity.Property(e => e.FieldHelpLink).HasMaxLength(200);

            entity.Property(e => e.FieldLabel).HasMaxLength(100);

            entity.Property(e => e.FieldPopupText).HasMaxLength(100);

            entity.Property(e => e.FormId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.InternalName).HasMaxLength(20);

            entity.Property(e => e.IsPii).HasColumnName("IsPII");

            entity.Property(e => e.OldFieldId).HasColumnName("oldFieldId");

            entity.Property(e => e.RegExValMsg).HasMaxLength(200);

            entity.Property(e => e.RegExValidator).HasMaxLength(200);
        });

        modelBuilder.Entity<MdpAppProcessFields>(entity =>
        {
            entity.HasKey(e => new { e.AppTypeId, e.ProcessFieldId });

            entity.ToTable("mdpAppProcessFields");

            entity.HasOne(d => d.ProcessField)
                .WithMany(p => p.MdpAppProcessFields)
                .HasForeignKey(d => d.ProcessFieldId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mdpAppProcessFields_ProcessFields");

        });

        modelBuilder.Entity<MdpAttachmentTypes>(entity =>
        {
            entity.Property(e => e.AttachmentTypeId).HasDefaultValueSql("(newid())");
        });

        modelBuilder.Entity<MdpAppSectionFields>(entity =>
        {
            entity.HasKey(e => e.SectionFieldId);

            entity.ToTable("mdpAppSectionFields");

            entity.HasIndex(e => e.FieldId)
                .HasDatabaseName("IX_AppSectionFields_FieldId");

            entity.HasIndex(e => e.SectionId)
                .HasDatabaseName("IX_AppSectionFields_SectionId");

            entity.Property(e => e.SectionFieldId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.HelpLink).HasMaxLength(255);

            entity.Property(e => e.IsPii).HasColumnName("IsPII");

            entity.Property(e => e.Label).HasMaxLength(100);

            entity.Property(e => e.PopupText).HasMaxLength(500);

            entity.Property(e => e.RegExValMsg).HasMaxLength(200);

            entity.Property(e => e.RegExValidator).HasMaxLength(200);

            entity.HasOne(d => d.Field)
                .WithMany(p => p.MdpAppSectionFields)
                .HasForeignKey(d => d.FieldId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mdpAppSectionFields_MasterFields");

            entity.HasOne(d => d.FieldType)
                .WithMany(p => p.MdpAppSectionFields)
                .HasForeignKey(d => d.FieldTypeId)
                .HasConstraintName("FK_mdpAppSectionFields_mdpFieldTypes");

            entity.HasOne(d => d.Section)
                .WithMany(p => p.MdpAppSectionFields)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mdpAppSectionFields_mdpAppSections");
        });

        modelBuilder.Entity<MdpAppSections>(entity =>
        {
            entity.HasKey(e => e.SectionId);

            entity.ToTable("mdpAppSections");

            entity.HasIndex(e => e.AppTypeStateId)
                .HasDatabaseName("IX_mdpAppSections_AppTypeId");

            entity.Property(e => e.SectionId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.SectionTitle)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(d => d.AppTypeState)
                .WithMany(p => p.MdpAppSections)
                .HasForeignKey(d => d.AppTypeStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mdpAppSections_mdpAppTypeStates");
        });

        modelBuilder.Entity<MdpAppTypeCodes>(entity =>
        {
            entity.HasKey(e => new { e.VendorId, e.AppType });

            entity.ToTable("mdpAppTypeCodes");

            entity.Property(e => e.AppType).HasMaxLength(5);

            entity.Property(e => e.AliasForAppType).HasMaxLength(5);

            entity.Property(e => e.AutoImsenabled).HasColumnName("AutoIMSEnabled");

            entity.Property(e => e.QueueName).HasMaxLength(50);

            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<MdpAppTypes>(entity =>
        {
            entity.HasKey(e => e.AppTypeId);

            entity.ToTable("mdpAppTypes");

            entity.HasIndex(e => new { e.VendorId, e.AppType })
                .HasDatabaseName("UK_mdpAppTypes")
                .IsUnique();

            entity.Property(e => e.AppTypeId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.AliasForAppType).HasMaxLength(5);

            entity.Property(e => e.AppType)
                .IsRequired()
                .HasMaxLength(5);

            entity.Property(e => e.AutoIMSEnabled).HasColumnName("AutoIMSEnabled");

            entity.Property(e => e.QueueName).HasMaxLength(50);

            entity.Property(e => e.Title).HasMaxLength(100);

            //entity.HasOne(d => d.AppTypeNavigation)
            //    .WithOne(p => p.InverseAppTypeNavigation)
            //    .HasForeignKey<MdpAppTypes>(d => d.AppTypeId)
            //    .OnDelete(DeleteBehavior.ClientSetNull)
            //    .HasConstraintName("FK_mdpAppTypes_mdpAppTypes");
        });

        modelBuilder.Entity<MdpAppTypeAttachmentTypes>(entity =>
        {
            entity.Property(e => e.AppTypeAttachmentTypeId).HasDefaultValueSql("(newid())");

            //entity.HasOne(d => d.AppTypeState)
            //    .WithMany(p => p.MdpAppTypeAttachmentTypes)
            //    .HasForeignKey(d => d.AppTypeStateId)
            //    .OnDelete(DeleteBehavior.ClientSetNull)
            //    .HasConstraintName("FK_mdpAppTypeAttachmentTypes_mdpAppTypeStates");

            //entity.HasOne(d => d.AttachmentType)
            //    .WithMany(p => p.MdpAppTypeAttachmentTypes)
            //    .HasForeignKey(d => d.AttachmentTypeId)
            //    .OnDelete(DeleteBehavior.ClientSetNull)
            //    .HasConstraintName("FK_mdpAppTypeAttachmentTypes_mdpAttachmentTypes");
        });

        modelBuilder.Entity<MdpAppTypeStates>(entity =>
        {
            entity.HasKey(e => e.AppTypeStateId);

            entity.ToTable("mdpAppTypeStates");

            entity.HasIndex(e => new { e.AppTypeId, e.AppState })
                .HasDatabaseName("IX_mdpAppTypeStates")
                .IsUnique();

            entity.Property(e => e.AppTypeStateId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.AppState)
                .IsRequired()
                .HasMaxLength(2);

            entity.HasOne(d => d.AppType)
                .WithMany(p => p.AppTypeStates)
                .HasForeignKey(d => d.AppTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mdpAppTypeStates_mdpAppTypes");
        });

        modelBuilder.Entity<MdpFieldTypes>(entity =>
        {
            entity.HasKey(e => e.FieldTypeId);

            entity.ToTable("mdpFieldTypes");

            entity.Property(e => e.FieldTypeId).HasColumnName("fieldTypeId");

            entity.Property(e => e.RegExValidator).HasMaxLength(100);

            entity.Property(e => e.TypeDesc)
                .HasColumnName("typeDesc")
                .HasMaxLength(100);
        });

        modelBuilder.Entity<PdfTemplate>(entity =>
        {
            entity.Property(e => e.TemplateId).HasDefaultValueSql("(newid())");
            //entity.HasOne(d => d.AppTypeState)
            //    .WithMany(p => p.PdfTemplates)
            //    .HasForeignKey(d => d.AppTypeStateId)
            //    .OnDelete(DeleteBehavior.ClientSetNull)
            //    .HasConstraintName("FK_PdfTemplate_mdpAppTypeStates");
        });

        modelBuilder.Entity<PendingSignBatch>(entity =>
        {
            entity.HasKey(e => e.BatchId)
                .IsClustered(false);

            entity.HasIndex(e => e.Id)
                .HasDatabaseName("CIX_PendingSignBatch")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.BatchId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.DateRequested).HasDefaultValueSql("(sysdatetime())");

            entity.Property(e => e.FormCode)
                .HasColumnName("formCode")
                .HasMaxLength(20);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.VendorId).HasColumnName("vendorId");
        });

        modelBuilder.Entity<PendingSignRequest>(entity =>
        {
            entity.HasIndex(e => new { e.BatchId, e.RequestId })
                .HasDatabaseName("UK_PendingSignRequest")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");

            entity.HasOne(d => d.Batch)
                .WithMany(p => p.PendingSignRequest)
                .HasForeignKey(d => d.BatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PendingSignRequest_PendingSignBatch");

            entity.HasOne(d => d.Request)
                .WithMany(p => p.PendingSignRequest)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PendingSignRequest_Requests");
        });

        modelBuilder.Entity<PoliceAgency>(entity =>
        {
            entity.HasIndex(e => e.Agency);

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Agency)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.City)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.NameAndAddress)
                .IsRequired()
                .HasMaxLength(367)
                .HasComputedColumnSql("(concat(rtrim([Agency]),N', ',rtrim([Street]),', ',rtrim([City]),', ',rtrim([State]),' ',[Zip]))");

            entity.Property(e => e.State)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.Street)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.Zip)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PoliceDept>(entity =>
        {
            entity.Property(e => e.PoliceDeptId).HasColumnName("Police_Dept_ID");

            entity.Property(e => e.City)
                .HasMaxLength(25)
                .IsUnicode(false);

            entity.Property(e => e.CityId).HasColumnName("City_ID");

            entity.Property(e => e.County)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(e => e.PoliceDeptName)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.State)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(e => e.StreetAddress)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(e => e.TypeofDepartment)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(e => e.ZipCode)
                .IsRequired()
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PreferredVendor>(entity =>
        {
            entity.HasKey(e => new { e.GroupId, e.VendorId });
        });

        modelBuilder.Entity<ProcessFields>(entity =>
        {
            entity.Property(e => e.ProcessFieldId).HasDefaultValueSql("(newid())");
#if false
// attributes on entity
            entity.HasKey(e => e.ProcessFieldId);

            entity.Property(e => e.DisplayName).HasMaxLength(100);

            entity.Property(e => e.DotNetType).HasMaxLength(100);

            entity.Property(e => e.FieldName)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.SqlFieldName).HasMaxLength(50);

            entity.Property(e => e.SqlType).HasMaxLength(100);
#endif
        });

        modelBuilder.Entity<ProcessStages>(entity =>
        {
            entity.HasKey(e => e.ProcessStageId);

            entity.Property(e => e.ProcessStageId)
                .HasColumnName("ProcessStageID")
                .ValueGeneratedNever();

            entity.Property(e => e.ProcessStageCode).HasMaxLength(20);

            entity.Property(e => e.ProcessStageName)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<RequestAttachments>(entity =>
        {
            entity.HasKey(e => e.AttachmentId)
                .IsClustered(false);

            entity.HasIndex(e => e.AttachmentId)
                .HasDatabaseName("CIX_RequestAttachments_id")
                .IsClustered(false);

            entity.HasIndex(e => e.RequestId);

            entity.Property(e => e.AttachmentId)
                .HasColumnName("AttachmentID")
                .HasDefaultValueSql("(newid())");

            entity.Property(e => e.Filename).HasMaxLength(250);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.RequestId).HasColumnName("RequestID");

            entity.HasOne(d => d.Request)
                .WithMany(p => p.RequestAttachments)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestAttachments_Requests");

            entity.HasOne(d => d.UploadedByNavigation)
                .WithMany(p => p.RequestAttachments)
                .HasForeignKey(d => d.UploadedBy)
                .HasConstraintName("FK_RequestAttachments_Users");
        });
       
        modelBuilder.Entity<RequestDisbursement>(entity =>
        {
            entity.HasKey(e => e.id)
                .IsClustered(true);

            entity.ToTable("RequestDisbursement");

            entity.Property(e => e.id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.RequestId);
        });

        modelBuilder.Entity<RequestAttachmentReviewHistory>(entity =>
        {
        });

        modelBuilder.Entity<RequestLinks>(entity =>
        {
            entity.HasKey(e => new { e.RequestId, e.LinkId });

            entity.HasOne(d => d.Request)
                .WithMany(p => p.RequestLinks)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_RequestLinks_Requests");
        });

        modelBuilder.Entity<RequestNotes>(entity =>
        {
            entity.HasIndex(e => e.RequestId)
                .HasDatabaseName("IX_RequestNotes");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Timestamp)
                .HasColumnName("timestamp")
                .IsRowVersion();

            entity.Property(e => e.ClientRemarks)
   .HasColumnName("ClientRemarks")
   .HasMaxLength(4000) 
   .IsRequired(false); 

            entity.HasOne(d => d.ModifiedByNavigation)
                .WithMany(p => p.RequestNotes)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("FK_RequestNotes_Users");


            entity.HasOne(d => d.Request)
                .WithMany(p => p.RequestNotes)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestNotes_Requests");
        });

        modelBuilder.Entity<RequestPdf>(entity =>
        {
            entity.HasKey(e => e.RequestId)
                .IsClustered(false);

            entity.ToTable("RequestPDF");

            entity.HasIndex(e => e.Id)
                .HasDatabaseName("CIX_RequestPDF_RequestId")
                .IsClustered();

            entity.Property(e => e.RequestId).ValueGeneratedNever();

            entity.Property(e => e.DateCreated).HasDefaultValueSql("(sysdatetime())");

            entity.Property(e => e.FormCode).HasMaxLength(20);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Pdf).HasColumnName("PDF");

            entity.Property(e => e.TextSignature).HasMaxLength(100);

            entity.Property(e => e.TitleOfSigner).HasMaxLength(100);

            entity.Property(e => e.VendorId).HasColumnName("vendorId");

            entity.HasOne(d => d.Request)
                .WithOne(p => p.RequestPdf)
                .HasForeignKey<RequestPdf>(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestPDF_Requests");

            entity.HasOne(d => d.SignedByUser)
                .WithMany(p => p.RequestPdf)
                .HasForeignKey(d => d.SignedByUserId)
                .HasConstraintName("FK_RequestPDF_Users");
        });

        modelBuilder.Entity<Requests>(entity =>
        {
            entity.HasKey(e => e.RequestId)
                .IsClustered(false);

            entity.HasIndex(e => e.Auction);

            entity.HasIndex(e => e.FileUploadId);

            entity.HasIndex(e => e.GroupId);

            entity.HasIndex(e => e.Id)
                .HasDatabaseName("CIX_Requests_Id")
                .IsClustered();

            entity.HasIndex(e => e.LienholderName)
                .HasDatabaseName("IX_Requests_Lienholder");

            entity.HasIndex(e => e.UserId);

            entity.HasIndex(e => e.VehicleMake);

            entity.HasIndex(e => e.VehicleYear);

            entity.HasIndex(e => e.VendorId);

            entity.HasIndex(e => e.Vin)
                .HasDatabaseName("IX_Requests_Vin");

            entity.HasIndex(e => new { e.AppType, e.Auction, e.Code, e.Courier, e.DateFromDmv, e.DatePrinted, e.DateReceived, e.DateShipped, e.DateTitleIssued, e.DateToDmv, e.DateToVendor, e.DmvCourier, e.Eta, e.GroupId, e.Last6Vin, e.LienholderName, e.ProcessStageId, e.RequestId, e.State, e.StatusId, e.ToVendorCourier, e.ToVendorTracking, e.TrackingNumber, e.UserId, e.VehicleMake, e.VehicleYear, e.VendorId, e.Vin })
                .HasDatabaseName("nci_wi_Requests_3C7F8B1277C787BD9B1EEAC23D456CDB");

            entity.HasIndex(e => new { e.AppType, e.Auction, e.Code, e.Courier, e.CourierId, e.DateFromDmv, e.DatePrinted, e.DateReceived, e.DateShipped, e.DateTitleIssued, e.DateToDmv, e.DateToVendor, e.DirectToVendor, e.DmvCourier, e.DmvCourierId, e.DmvTrackingNumber, e.Eta, e.FileUploadId, e.GroupId, e.JRequest, e.Last6Vin, e.LienholderName, e.Odometer, e.RequestId, e.State, e.ToVendorCourier, e.ToVendorTracking, e.TrackingNumber, e.UserId, e.VehicleMake, e.VehicleYear, e.ProcessStageId, e.StatusId, e.VendorId, e.Vin })
                .HasDatabaseName("IX_Requests_StageStatusVendorVin");

            entity.Property(e => e.RequestId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.AppType)
                .HasMaxLength(5)
                .HasDefaultValueSql("(N'RT')");

            entity.Property(e => e.Auction)
                .HasMaxLength(100)
                .HasComputedColumnSql("(CONVERT([nvarchar](100),json_value([jRequest],'$.\"Auctioneer\"')))");

            entity.Property(e => e.Code).HasMaxLength(10);

            entity.Property(e => e.Courier).HasMaxLength(50);

            entity.Property(e => e.DmvCourier).HasMaxLength(50);

            entity.Property(e => e.DmvTrackingNumber).HasMaxLength(50);

            entity.Property(e => e.Eta).HasColumnName("ETA");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.JRequest)
                .HasColumnName("jRequest")
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Last6Vin)
                .HasColumnName("Last6VIN")
                .HasMaxLength(6)
                .HasComputedColumnSql("(CONVERT([nchar](6),right(CONVERT([nchar](17),json_value([jRequest],'$.\"Vehicle Vin\"')),(6))))");

            entity.Property(e => e.LI_ToDmvCourier)
                .HasMaxLength(50);

            entity.Property(e => e.LI_ToDmvTracking)
                .HasMaxLength(50);

            entity.Property(e => e.LienholderName)
                .HasMaxLength(100)
                .HasComputedColumnSql("(CONVERT([nvarchar](100),coalesce(json_value([jRequest],'$.\"Lien Holder Name\"'),json_value([jRequest],'$.\"Lien holder Name\"'))))");

            entity.Property(e => e.Odometer)
                .HasMaxLength(10)
                .HasComputedColumnSql("(CONVERT([nvarchar](10),json_value([jRequest],'$.\"Vehicle Odometer\"')))");

            entity.Property(e => e.ProcessStageId).HasColumnName("ProcessStageID");

            entity.Property(e => e.State)
                .HasMaxLength(2)
                .HasComputedColumnSql("(coalesce(CONVERT([nchar](2),json_value([jRequest],'$.\"AppTypeState\"')),CONVERT([nchar](2),json_value([jRequest],'$.State'))))");

            entity.Property(e => e.StatusId).HasColumnName("StatusID");

            entity.Property(e => e.ToVendorCourier).HasMaxLength(50);

            entity.Property(e => e.ToVendorTracking).HasMaxLength(50);

            entity.Property(e => e.TrackingNumber).HasMaxLength(50);

            entity.Property(e => e.VehicleMake)
                .HasMaxLength(50)
                .HasComputedColumnSql("(CONVERT([nvarchar](50),json_value([jRequest],'$.\"Vehicle Make\"')))");

            entity.Property(e => e.VehicleYear)
                .HasMaxLength(4)
                .HasComputedColumnSql("(CONVERT([nvarchar](4),json_value([jRequest],'$.\"Vehicle Year\"')))");

            entity.Property(e => e.Vin)
                .HasColumnName("VIN")
                .HasMaxLength(17)
                .HasComputedColumnSql("(CONVERT([nchar](17),json_value([jRequest],'$.\"Vehicle Vin\"')))");

            entity.HasOne(d => d.Group)
                .WithMany(p => p.Requests)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK_Requests_Groups");

            entity.HasOne(d => d.Status)
                .WithMany(p => p.Requests)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("FK_Requests_Status");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Requests)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Requests_Users");

            entity.HasOne(d => d.Vendor)
                .WithMany(p => p.Requests)
                .HasForeignKey(d => d.VendorId)
                .HasConstraintName("FK_Requests_Vendors");
        });

        modelBuilder.Entity<RequestSigning>(entity =>
        {
            entity.HasIndex(e => e.Id)
                .HasDatabaseName("UK_RequestSigning_RequestId_SigningId")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");

            entity.HasOne(d => d.Request)
                .WithMany(p => p.RequestSigning)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestSigning_Requests");

            entity.HasOne(d => d.Signing)
                .WithMany(p => p.RequestSigning)
                .HasForeignKey(d => d.SigningId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestSigning_Signings");
        });

        modelBuilder.Entity<RequestTracking>(entity =>
        {
            entity.HasKey(e => new { e.RequestId, e.ChangeDate });

            entity.Property(e => e.JRequest_After).HasColumnName("jRequest_After");

            entity.Property(e => e.JRequest_Before).HasColumnName("jRequest_Before");

            entity.Property(e => e.JRequestDiff).HasColumnName("jRequestDiff");

            entity.Property(e => e.ProcessStageId).HasColumnName("ProcessStageID");

            entity.Property(e => e.JProcess_After).HasColumnName("jProcess_After");

            entity.Property(e => e.JProcess_Before).HasColumnName("jProcess_Before");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
        });

        modelBuilder.Entity<ShipmentDetails>(entity =>
        {
            entity.HasKey(e => new { e.ShipmentId, e.SortOrder });

            entity.HasIndex(e => e.RequestId);

            entity.HasOne(d => d.Request)
                .WithMany(p => p.ShipmentDetails)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShipmentDetails_Requests");

            entity.HasOne(d => d.Shipment)
                .WithMany(p => p.ShipmentDetails)
                .HasForeignKey(d => d.ShipmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShipmentDetails_Shipments");
        });

        modelBuilder.Entity<Shipments>(entity =>
        {
            entity.HasKey(e => e.ShipmentId);

            entity.Property(e => e.ShipmentId).ValueGeneratedNever();

            entity.Property(e => e.AuctionName).HasMaxLength(100);

            entity.Property(e => e.Courier).HasMaxLength(50);

            entity.Property(e => e.CreatedBy).HasDefaultValueSql("(CONVERT([uniqueidentifier],session_context(N'userid')))");

            entity.Property(e => e.DateCreated)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())");

            entity.Property(e => e.DateShipped).HasColumnType("date");

            entity.Property(e => e.GroupName).HasMaxLength(100);

            entity.Property(e => e.TrackingNumber).HasMaxLength(50);

            entity.HasOne(d => d.Vendor)
                .WithMany(p => p.Shipments)
                .HasForeignKey(d => d.VendorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shipments_Vendors");
        });

        modelBuilder.Entity<Signings>(entity =>
        {
            entity.HasKey(e => e.SigningId)
                .IsClustered(false);

            entity.HasIndex(e => e.Id)
                .HasDatabaseName("CIX_Signings_id")
                .IsUnique()
                .IsClustered();

            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.SigningId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Signature)
                .IsRequired()
                .HasMaxLength(500);
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.Property(e => e.StatusId)
                .HasColumnName("StatusID")
                .ValueGeneratedNever();

            entity.Property(e => e.StatusName)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<SysAdmins>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.Property(e => e.UserId).ValueGeneratedNever();

            entity.Property(e => e.DateAdded).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<Uicolumns>(entity =>
        {
            entity.HasKey(e => e.ColumnId);

            entity.ToTable("UIColumns");

            entity.Property(e => e.ColumnId)
                .ValueGeneratedNever();

            entity.Property(e => e.ClassName).HasMaxLength(512);

            entity.Property(e => e.ColumnName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ColumnTitle).HasMaxLength(50);

            entity.Property(e => e.Code).HasMaxLength(100);
        });

        modelBuilder.Entity<UiviewColumns>(entity =>
        {
            entity.HasKey(e => new { e.ViewId, e.ColumnId });

            entity.ToTable("UIViewColumns");

            entity.HasOne(d => d.Column)
                .WithMany(p => p.UiviewColumns)
                .HasForeignKey(d => d.ColumnId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UIViewColumns_UIColumns");

            entity.HasOne(d => d.View)
                .WithMany(p => p.UiviewColumns)
                .HasForeignKey(d => d.ViewId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UIViewColumns_UIViews");
        });

        modelBuilder.Entity<Uiviews>(entity =>
        {
            entity.HasKey(e => e.ViewId);

            entity.ToTable("UIViews");

            entity.Property(e => e.ViewId)
                .HasColumnName("ViewID")
                .ValueGeneratedNever();

            entity.Property(e => e.IsVendorView).HasColumnName("isVendorView");

            entity.Property(e => e.Title).HasMaxLength(100);

            entity.Property(e => e.ViewName)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<UserGroups>(entity =>
        {
            entity.HasKey(e => new { e.GroupId, e.UserId });

            entity.HasOne(d => d.Group)
                .WithMany(p => p.UserGroups)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserGroups_Groups");

            entity.HasOne(d => d.User)
                .WithMany(p => p.UserGroups)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserGroups_Users");
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_Users");

            entity.HasIndex(e => e.UserPrincipalName)
                .HasDatabaseName("UK_Users_UserId")
                .IsUnique();

            entity.Property(e => e.UserId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.ClaimIdp)
                .HasColumnName("claim_idp")
                .HasMaxLength(255);

            entity.Property(e => e.ClaimIss)
                .HasColumnName("claim_iss")
                .HasMaxLength(255);

            entity.Property(e => e.ClaimOid)
                .HasColumnName("claim_oid")
                .HasMaxLength(255);

            entity.Property(e => e.ClaimSub)
                .HasColumnName("claim_sub")
                .HasMaxLength(255);

            entity.Property(e => e.DisplayName).HasMaxLength(100);

            entity.Property(e => e.Email).HasMaxLength(450);

            entity.Property(e => e.IsVendorAgent).HasComputedColumnSql("([dbo].[fn_IsVendorAgent]([UserId]))");

            entity.Property(e => e.NameIdentifierClaim).HasMaxLength(200);

            entity.Property(e => e.UserPrincipalName)
                .IsRequired()
                .HasMaxLength(256);
        });

        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.Property(e => e.UserId).ValueGeneratedNever();

            entity.Property(e => e.JSettings).HasColumnName("jSettings");
        });

        modelBuilder.Entity<AuditResponse>(entity =>
        {
            entity.HasKey(e => e.RequestNo);
            entity.Property(e => e.Outcome).HasMaxLength(1000);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Internal).HasMaxLength(1000);
            entity.Property(e => e.AssignedUser).HasMaxLength(1000);
            entity.Property(e => e.AssignedBy).HasMaxLength(1000);
        });

        modelBuilder.Entity<AuditFollowUpResponse>(entity =>
        {
            entity.HasKey(e => e.FollowUpId);
            entity.Property(e => e.Outcome).HasMaxLength(1000);
            entity.Property(e => e.AuditNotes).HasMaxLength(1000);
            entity.Property(e => e.Internal).HasMaxLength(1000);
            entity.Property(e => e.AssignedUser).HasMaxLength(1000);
            entity.Property(e => e.AssignedBy).HasMaxLength(1000);
        });

        modelBuilder.Entity<AuditCompleteResponse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RT_LH).HasColumnName("RT-LH");
            entity.Property(e => e.DT_LH_Name).HasColumnName("DT-LH-Name");
            entity.Property(e => e.LH_SToV).HasColumnName("LH-SToV");

        });

        modelBuilder.Entity<AuditBatch>(entity =>
        {
            entity.Property(e => e.AuditBatchId);
        });

        modelBuilder.Entity<AuditOutcomeComboboxData>(entity =>
        {
            entity.HasNoKey();
            entity.Property(e => e.Name).HasMaxLength(200);
        });


        modelBuilder.Entity<UploadedOCRDocuments>(entity =>
        {
            entity.HasKey(e => e.Id); // Configure the primary key
            entity.Property(e => e.RequestId).HasMaxLength(50);
        });


        modelBuilder.Entity<UserViews>(entity =>
        {
            entity.HasKey(e => e.ViewId);

            entity.Property(e => e.ViewId).ValueGeneratedNever();

            entity.Property(e => e.DataSource).HasMaxLength(100);

            entity.Property(e => e.ViewName).HasMaxLength(50);
        });

        modelBuilder.Entity<VendorAgent>(entity =>
        {
            entity.HasKey(e => new { e.VendorId, e.AgentId });

            entity.HasOne(d => d.Agent)
                .WithMany(p => p.VendorAgent)
                .HasForeignKey(d => d.AgentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VenderAgent_Users");

            entity.HasOne(d => d.Vendor)
                .WithMany(p => p.VendorAgent)
                .HasForeignKey(d => d.VendorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VenderAgent_Vendors");
        });

        modelBuilder.Entity<VendorFormFill>(entity =>
        {
            entity.HasIndex(e => new { e.VendorId, e.FormCode })
                .HasDatabaseName("UK_VendorFormFill")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.AppType).HasMaxLength(5);

            entity.Property(e => e.AppTypeState).HasMaxLength(2);

            entity.Property(e => e.FormCode)
                .IsRequired()
                .HasColumnName("formCode")
                .HasMaxLength(20);

            entity.Property(e => e.JData).HasColumnName("jData");

            entity.Property(e => e.VendorId).HasColumnName("vendorId");

            entity.HasOne(d => d.Vendor)
                .WithMany(p => p.VendorFormFill)
                .HasForeignKey(d => d.VendorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VendorFormFill_Vendors");
        });

        modelBuilder.Entity<VendorInvite>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.Property(e => e.DateCreated).HasDefaultValueSql("(sysdatetime())");

            entity.Property(e => e.UserEmail)
                .IsRequired()
                .HasMaxLength(100);
        });

        modelBuilder.Entity<Vendors>(entity =>
        {
            entity.HasKey(e => e.VendorId);

            entity.HasIndex(e => e.VendorCode)
                .HasDatabaseName("UK_Vendors_VendorCode")
                .IsUnique();

            entity.HasIndex(e => e.VendorId)
                .HasDatabaseName("UK_Vendors")
                .IsUnique();

            entity.Property(e => e.VendorId).HasDefaultValueSql("(newid())");

            entity.Property(e => e.VendorCode)
                .IsRequired()
                .HasMaxLength(5);

            entity.Property(e => e.VendorName)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<VendorSettings>(entity =>
        {
            entity.HasKey(e => e.VendorId);

            entity.Property(e => e.VendorId).ValueGeneratedNever();

            entity.Property(e => e.JSettings).HasColumnName("jSettings");
        });

        modelBuilder.Entity<VendorState>(entity =>
        {
            entity.HasIndex(e => new { e.State, e.VendorId })
                .HasDatabaseName("IX_VendorState")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.State)
                .IsRequired()
                .HasMaxLength(2);
        });

        modelBuilder.Entity<ViewDefinitions>(entity =>
        {
            entity.HasKey(e => e.ViewName);

            entity.Property(e => e.ViewName)
                .HasMaxLength(50)
                .ValueGeneratedNever();
        });

        modelBuilder.Entity<Vindetail>(entity =>
        {
            entity.ToTable("VINDetail");

            entity.HasIndex(e => e.Vin)
                .HasDatabaseName("IX_VINDetail")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.JsonDetails).HasColumnName("jsonDetails");

            entity.Property(e => e.Vin)
                .HasColumnName("VIN")
                .HasMaxLength(17)
                .HasComputedColumnSql("(CONVERT([nchar](17),json_value([jsonDetails],'$.VIN')))");
        });

        modelBuilder.Entity<VinPartialDetail>(entity =>
        {
            entity.HasKey(e => e.VinId);

            entity.HasIndex(e => e.VinPattern)
                .HasDatabaseName("IX_VinPartialDetail");

            entity.Property(e => e.VinId).HasColumnName("Vin_ID");

            entity.Property(e => e.CurbWeight).HasMaxLength(5);

            entity.Property(e => e.Cylinders).HasMaxLength(2);

            entity.Property(e => e.Doors).HasMaxLength(2);

            entity.Property(e => e.FuelType).HasMaxLength(12);

            entity.Property(e => e.Make).HasMaxLength(24);

            entity.Property(e => e.Model).HasMaxLength(32);

            entity.Property(e => e.VinPattern)
                .HasColumnName("Vin_PATTERN")
                .HasMaxLength(10);

            entity.Property(e => e.Year).HasMaxLength(4);
        });

        modelBuilder.Entity<ZipCodes>(entity =>
        {
            entity.HasIndex(e => e.ZipCode)
                .HasDatabaseName("IX_ZipCodes");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.City)
                .HasMaxLength(35)
                .IsUnicode(false);

            entity.Property(e => e.County)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.Property(e => e.Latitude).HasColumnType("decimal(18, 6)");

            entity.Property(e => e.Longitude).HasColumnType("decimal(18, 6)");

            entity.Property(e => e.StateAbbr)
                .HasMaxLength(2)
                .IsUnicode(false);

            entity.Property(e => e.StateName)
                .HasMaxLength(35)
                .IsUnicode(false);

            entity.Property(e => e.ZipCode)
                .IsRequired()
                .HasMaxLength(5)
                .IsUnicode(false);
        });

        modelBuilder.Entity<FollowUpTags>(entity =>
        {
            entity.HasKey(e => new { e.FollowUpId, e.TagId });

            entity.HasIndex(e => e.TagId);

            entity.HasOne(d => d.FollowUp)
                .WithMany(p => p.FollowUpTags)
                .HasPrincipalKey(p => p.FollowUpId)
                .HasForeignKey(d => d.FollowUpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FollowUpTags_RequestFollowUps");
        });

        modelBuilder.Entity<FollowUpContacts>(entity =>
        {
            entity.HasKey(e => new { e.FollowUpId, e.ContactId });

            entity.HasIndex(e => e.ContactId);

            entity.HasOne(d => d.FollowUp)
                .WithMany(p => p.FollowUpContacts)
                .HasPrincipalKey(p => p.FollowUpId)
                .HasForeignKey(d => d.FollowUpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FollowUpContacts_RequestFollowUps");
        });

        modelBuilder.Entity<RequestCode>(entity =>
        {
            entity.HasIndex(e => new { e.RequestId, e.TagId });

            entity.HasIndex(e => e.TagId);

            entity.HasOne(d => d.Request)
                .WithMany(p => p.RequestCodes)
                .HasPrincipalKey(p => p.RequestId)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestCodes_Requests");

            //entity.HasOne(d => d.Tag)
            //    .WithMany(p => p.RequestCodes)
            //    .HasForeignKey(d => d.TagId)
            //    .OnDelete(DeleteBehavior.ClientSetNull)
            //    .HasConstraintName("FK_RequestCodes_Tag");
        });

        modelBuilder.Entity<RequestCancellation>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.Property(e => e.RequestId).ValueGeneratedNever();

            entity.HasOne(d => d.Request)
                .WithOne(p => p.RequestCancellation)
                .HasForeignKey<RequestCancellation>(d => d.RequestId)
                .HasConstraintName("FK_RequestCancellation_Requests");
        });

        modelBuilder.Entity<RequestFollowUps>(entity =>
        {
            entity.HasKey(e => new { e.RequestId, e.CreatedDate });

            entity.HasIndex(e => e.DueDate);

            entity.HasIndex(e => e.FollowUpId)
                .HasDatabaseName("IX_RequestFollowUps")
                .IsUnique();

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");

            entity.Property(e => e.Code).HasMaxLength(50);

            entity.Property(e => e.FollowUpId).ValueGeneratedOnAdd();

            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<RequestFollowUpHistory>(entity =>
        {
            entity.HasKey(e => new { e.RequestId, e.ModifiedDate })
                  .HasName("IX_RequestFollowUpHistory_RequestId_ModifiedDate");

            entity.Property(e => e.Code).HasMaxLength(50);

            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId)
                .IsClustered(false);

            entity.HasIndex(e => new { e.VendorId, e.TagId })
                .HasDatabaseName("CI_Tag")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.TagName)
                .IsRequired();

            entity.Property(e => e.TagType).HasMaxLength(10);
        });

        modelBuilder.Entity<TagCategory>(entity =>
        {
            entity.HasKey(e => e.TagCategoryId)
                .IsClustered(true);

            entity.Property(e => e.TagCategoryName).HasMaxLength(50);
        });

        modelBuilder.Entity<GroupProfile>(entity =>
        {
            entity.HasKey(e => e.GroupProfileID)
                .IsClustered(true);
            entity.Property(e => e.GroupProfileID).HasDefaultValueSql("(newid())");
            entity.Property(e => e.JRequest)
                .HasColumnName("jRequest")
                .HasColumnType("nvarchar(max)");
            entity.HasOne(d => d.GroupProfileType)
                .WithMany(p => p.GroupProfiles)
                .HasForeignKey(d => d.GroupProfileTypeID)
                .HasConstraintName("FK_GroupProfile_GroupProfileType");
            entity.HasOne(d => d.Group)
                .WithMany(p => p.GroupProfiles)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK_GroupProfile_Groups");
        });

        modelBuilder.Entity<GroupProfileType>(entity =>
        {
            entity.HasKey(e => e.GroupProfileTypeID)
                .IsClustered(true);
            entity.Property(e => e.GroupProfileTypeID).HasDefaultValueSql("(newid())");
            entity.Property(e => e.GroupProfileTypeName).HasColumnName("GroupProfileTypeName");
        });

        modelBuilder.Entity<GroupProfileTypeUsage>(entity =>
        {
            entity.HasKey(e => e.GroupProfileTypeUsageID)
                .IsClustered(true);
            entity.Property(e => e.GroupProfileTypeUsageID).HasDefaultValueSql("(newid())");
            entity.Property(e => e.AppType)
                .HasMaxLength(5);
            entity.Property(e => e.AppTypeState).HasMaxLength(2);
            entity.Property(e => e.Include).HasColumnName("Include");
            entity.Property(e => e.Exclude).HasColumnName("Exclude");
            entity.HasOne(d => d.GroupProfileType)
                .WithMany(p => p.GroupProfileTypeUsages)
                .HasForeignKey(d => d.GroupProfileTypeID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupProfileTypeUsage_GroupProfileType");
        });

        modelBuilder.Entity<ProposedUpdates>(entity =>
        {
            entity.ToTable("ProposedUpdates");

            entity.HasKey(e => e.ProposedUpdateId);

            entity.Property(e => e.ProposedUpdateId)
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.RequestId)
                  .IsRequired();

            entity.Property(e => e.CreatedDate)
                  .IsRequired();

            entity.Property(e => e.CreatedBy)
                  .IsRequired();

            entity.Property(e => e.jRequest_Before)
                  .IsRequired();

            entity.Property(e => e.jRequest_Proposed)
                  .IsRequired();

            entity.Property(e => e.ApprovalStatus)
                  .HasColumnType("BIT");
        });

        modelBuilder.Entity<UploadLinks>(entity =>
        {
            entity.ToTable("UploadLinks");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.RequestId).IsRequired();

            entity.Property(e => e.CreatedDate);

            entity.Property(e => e.ExpirationDate);

            entity.Property(e => e.CreatedBy);

            entity.Property(e => e.AttachmentTypeId);

            entity.Property(e => e.AttachmentId);

            entity.Property(e => e.IsUsed)
                  .HasColumnType("BIT");

            entity.Property(e => e.IsDocumentUploaded)
                  .HasColumnType("BIT");
        });

        modelBuilder
           .Entity<RequestStatus>().HasNoKey().ToView(MyDMVpro.Models.RequestStatus.RequestStatusViewName);
        modelBuilder
           .Entity<Requests>().ToTable("Requests");
        modelBuilder
           .Entity<InvoiceStatus>().HasNoKey().ToView("vw_InvoiceStatus_v2");
        modelBuilder
           .Entity<FormAnalyzerUploads>().HasNoKey().ToView("vw_FormAnalyzerUploads_v2");
        modelBuilder
           .Entity<FormAnalyzerForms>().HasNoKey().ToView("vw_FormAnalyzerForms");
        modelBuilder
           .Entity<ChatStatus>().HasNoKey().ToView("vw_ChatStatus");
        modelBuilder
           .Entity<ChatMessages>().HasNoKey().ToView("vw_UnreadChats_v2");

        modelBuilder
          .Entity<CommunicationChatMessages>().HasNoKey().ToView("vw_UnreadChatsForCommunication");

        modelBuilder
            .Entity<RequestChats>().HasNoKey().ToView("vw_RequestChats");
        modelBuilder
           .Entity<Shipment>().HasNoKey().ToView("vw_Shipments");
        modelBuilder
           .Entity<ShipmentDetail>().HasNoKey().ToView("vw_ShipmentDetails");
        modelBuilder
           .Entity<RequestFollowUpStatus>().HasNoKey().ToView("vw_FollowUpStatus_v2");

        modelBuilder
          .Entity<DocumentReceivedViewModel>().HasNoKey().ToView("vw_DocumentReceived_No_Request");

        modelBuilder
            .Entity<VendorGroupPaymentsAndDisbursements>().HasNoKey().ToView("vw_VendorGroupPaymentsAndDisbursements");

        modelBuilder
            .Entity<NotifyClientViewModal>().HasNoKey().ToView("vw_NotifyClientChatView");

        modelBuilder
            .Entity<PaymentsAndDisbursementHistory>().HasNoKey();

        modelBuilder
            .Entity<NeedMissingDataModel>().HasNoKey().ToView("vw_NeededDataRequestCodesView");

        modelBuilder.HasDbFunction(typeof(BaseMaggardDMVContext).GetMethod(nameof(GetAttachmentStatus), new[] { typeof(Guid) }))
            .HasName("fn_AttachmentStatus");

        modelBuilder.HasDbFunction(typeof(BaseMaggardDMVContext)
                        .GetMethod(nameof(JSON_VALUE), new[] { typeof(string), typeof(string) }))
                        .HasName("JSON_VALUE");

        modelBuilder.HasDbFunction(typeof(BaseMaggardDMVContext)
            .GetMethod(nameof(TryCastJsonValueAsGuid), new[] { typeof(string), typeof(string) }))
            .HasTranslation(args =>
            {
                // Get the SQL expression factory from the current DbContext's services.
                var sqlExpressionFactory = this.GetService<ISqlExpressionFactory>();

                // Build the JSON_VALUE call: JSON_VALUE(json, path)
                SqlExpression jsonValueCall = sqlExpressionFactory.Function(
                    "JSON_VALUE",
                    args,
                    nullable: true,
                    argumentsPropagateNullability: new[] { true, true },
                    returnType: typeof(string));

                // Create a SQL fragment that represents the TRY_CAST wrapping the JSON_VALUE call.
                // Because TRY_CAST is not directly available as a function,
                // we build the fragment manually.
                // (Note: In a production solution you may wish to build this more robustly.)
                var sql = $"TRY_CAST({jsonValueCall} AS uniqueidentifier)";
                return sqlExpressionFactory.Fragment(sql);
            });

        modelBuilder
           .Entity<AttachmentReview>().HasNoKey().ToView(MyDMVpro.Models.AttachmentReview.AttachmentReviewViewName);
        modelBuilder.Entity<NeedToProcessResponse>(entity =>
        {
            entity.HasKey(e => e.RequestNo);
            entity.Property(e => e.AssignedShipper).HasMaxLength(55);
            entity.Property(e => e.AssignedProcessor).HasMaxLength(55);
            entity.Property(e => e.Assignedby).HasMaxLength(255);
            entity.Property(e => e.NeedToProcessStageName).HasMaxLength(255);
        });
        modelBuilder.Entity<ProcessingDayEnum>(entity =>
        {
            entity.HasKey(e => e.dayid);
            entity.Property(e => e.daydesc);
        });
        modelBuilder
          .Entity<DataReview>().HasNoKey();

        modelBuilder.Entity<CalculationTables>(entity =>
        {
            entity.HasKey(e => e.Id); // Primary Key

            entity.Property(e => e.Id)
                  .ValueGeneratedOnAdd();

            entity.HasOne(p => p.states)
              .WithMany(s => s.CalculationTables)
              .HasForeignKey(p => p.State)
              .OnDelete(DeleteBehavior.NoAction);

        });
        
        modelBuilder.Entity<US_State>().ToTable("US_States");

        modelBuilder.Entity<NotifyClient>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(p => p.Request)
             .WithMany(s => s.NotifyClient)
             .HasForeignKey(p => p.RequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentTypes>(entity =>
        {
            entity.HasKey(e => e.PaymentTypeID);
           
        });

        modelBuilder.Entity<PaymentsAndDisbursement>(entity =>
        {
            entity.HasKey(e => e.PaymentID);
            
            entity.HasOne(p => p.PaymentTypes)
                  .WithMany(s => s.PaymentsAndDisbursement)
                  .HasForeignKey(p => p.PaymentTypeID)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<DocumentReceived>(entity =>
        {
            entity.HasKey(e => e.DocumentReceivedID);

            entity.HasOne(p => p.Group)
             .WithMany(s => s.DocumentReceived)
             .HasForeignKey(p => p.GroupId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StripePaymentModel>(entity =>
        {
            entity.HasKey(e => e.PaymentId);
        });

        modelBuilder.Entity<PaymentLink>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<FeaturePermission>(entity =>
        {
            entity.HasKey(e => e.Id);

        });

        modelBuilder.Entity<Feature>(entity =>
        {
            entity.HasKey(e => e.FeatureId);

        });
    }

    // This is a placeholder definition for EntityFramework to use for the user-defined function in SQL
    // https://learn.microsoft.com/en-us/ef/core/querying/user-defined-function-mapping
    public int GetAttachmentStatus(Guid requestID)
    {
        throw new NotImplementedException();
    }
    [DbFunction("JSON_VALUE", IsBuiltIn = true, IsNullable = false)]
    public string JSON_VALUE(string expression, string path) => throw new NotImplementedException();

    public Guid? TryCastJsonValueAsGuid(string json, string path)
    {
        // This method is never called directly. Its body is never executed.
        throw new NotSupportedException();
    }
}
