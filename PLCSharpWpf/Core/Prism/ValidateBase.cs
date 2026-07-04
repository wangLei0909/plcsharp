using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PLCSharp.Core.Prism
{
    /// <summary>
    /// ValidateBase
    /// </summary>
    public class ValidateBase : ViewModelBase, IDataErrorInfo
    {
        #region 属性

        private readonly Dictionary<string, string> dataErrors = [];

        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValidated
        {
            get
            {
                if (dataErrors != null && dataErrors.Count > 0)
                {
                    return false;
                }
                return true;
            }
        }

        #endregion 属性

        /// <summary>
        /// this[]
        /// </summary>
        /// <param name="columnName">column名称</param>
        public string this[string columnName]
        {
            get
            {
                ValidationContext vc = new(this, null, null)
                {
                    MemberName = columnName
                };
                var res = new List<ValidationResult>();
                var result = Validator.TryValidateProperty(GetType().GetProperty(columnName).GetValue(this, null), vc, res);
                if (res.Count > 0)
                {
                    AddDic(dataErrors, vc.MemberName);
                    return string.Join(Environment.NewLine, [.. res.Select(r => r.ErrorMessage)]);
                }
                RemoveDic(dataErrors, vc.MemberName);
                return null;
            }
        }

        /// <summary>
        /// 错误
        /// </summary>
        public string Error
        {
            get
            {
                return null;
            }
        }

        #region 附属方法

        private static void RemoveDic(Dictionary<string, string> dics, string dicKey)
        {
            dics.Remove(dicKey);
        }

        private static void AddDic(Dictionary<string, string> dics, string dicKey)
        {
            dics.TryAdd(dicKey, "");
        }

        #endregion 附属方法
    }
}