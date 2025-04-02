using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Common.ViewHelpers
{
    public class BaseListViewModel
    {
        public DatatableButtonCollection Buttons { get; set; }
        public DatatableColumnCollection Columns { get; set; }
        public ColumnSortInfoCollection SortColumns { get; set; }
        public int PageLength { get; set; }
        public string LengthMenu { get; set; }

        /// <summary>
        /// Set to true if the view should allow multiple instances of the same page
        /// </summary>
        public bool AllowMultiplePerPage { get; set; } = false;

        protected string _controllerName;
        protected string _prefix;

        protected string _listId;
        protected string _dataUrl;
        protected string _queueName;
        protected string _queueTitle;
        protected string _tableVarName;

        protected virtual string GetDefaultControllerName()
        {
            return "RequestStatus";
        }

        /// <summary>
        /// Name of controller containing the endpoint for the data
        /// default is RequestStatus
        /// </summary>
        public string ControllerName
        {
            get
            {
                return _controllerName ?? GetDefaultControllerName();
            }
            set
            {
                _controllerName = value;
            }
        }

        /// <summary>
        /// Defaults to DT_{QueueName} if not set
        /// If AllowMultiplePerPage is true, then it will be DT_{PageName}_{QueueName}
        /// </summary>
        public string ListId
        {
            get
            {
                if (_listId == null)
                {
                    if (AllowMultiplePerPage)
                    {
                        _listId = $"DT_{PageName}_{QueueName}";
                    }
                    else
                    {
                        _listId = $"DT_{QueueName}";
                    }
                }
                return _listId;
            }
            set
            {
                _listId = value;
            }
        }

        //public string DataUrl
        //{
        //    get
        //    {
        //        _dataUrl ??= $"/FollowUp/{QueueName}";
        //        return _dataUrl;
        //    }
        //    set
        //    {
        //        _dataUrl = value;
        //    }
        //}
        public string DataUrl
        {
            get
            {
                if (_dataUrl == null)
                {
                    List<string> segments = new();
                    foreach (var s in new string[] { Prefix, PageName, QueueName })
                    {
                        if (!string.IsNullOrWhiteSpace(s))
                            segments.Add(s);
                    }
                    // create a string by joining the segments with an underscore
                    string url = string.Join("_", segments);
                    _dataUrl = $"/{ControllerName}/{url}";
                }
                return _dataUrl;
            }
            set
            {
                _dataUrl = value;
            }
        }

        public string PageName { get; set; }

        public string QueueName
        {
            get
            {
                if (_queueName == null)
                {
                    return _queueTitle;
                }
                return _queueName;
            }
            set
            {
                _queueName = value;
            }
        }


        public string TableVarName
        {
            get
            {
                if (_tableVarName == null)
                {
                    if (_listId != null)
                    {
                        _tableVarName = _listId;
                    }
                    else if (string.IsNullOrEmpty(PageName))
                    {
                        _tableVarName = $"DT_{QueueName}";
                    }
                    else
                    {
                        _tableVarName = $"DT_{PageName}_{QueueName}";
                    }
                }
                return _tableVarName;
            }
            set
            {
                _tableVarName = value;
            }
        }

        /// <summary>
        /// Override to change the default prefix
        /// default prefix is "Vendor"
        /// </summary>
        /// <returns></returns>
        protected virtual string GetDefaultPrefix()
        {
            return "Vendor";
        }

        public string Prefix
        {
            get
            {
                if (_prefix == null)
                {
                    _prefix = GetDefaultPrefix();
                }
                return _prefix;
            }
            set
            {
                _prefix = value;
            }
        }

        public string QueueTitle
        {
            get
            {
                if (_queueTitle == null)
                {
                    return _queueName;
                }
                return _queueTitle;
            }
            set
            {
                _queueTitle = value;
            }
        }

    }
}
