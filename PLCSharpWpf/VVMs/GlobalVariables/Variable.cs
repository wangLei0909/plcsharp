using PLCSharp.Models;
using Prism.Mvvm;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLCSharp.VVMs.GlobalVariables
{
    /// <summary>
    /// 用户全局变量
    /// </summary>
    public class Variable : BindableBase
    {
        /// <summary>
        /// _DatasContext
        /// </summary>
        [NotMapped]
        public DatasContext _DatasContext { get; set; }
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();
        /// <summary>
        /// 配方标识
        /// </summary>
        public Guid RecipeID { get; set; }

        /// <summary>
        /// 变量名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Comment { get; set; }


        private dynamic _Value;
        /// <summary>
        /// 值
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 当前值
        /// </summary>
        public dynamic Value
        {
            get => _Value;
            set
            {
                if (!IsValidValue(value, Type)) return;


                if (RetainPersistent && value != null && _Value != value)
                {
                    SetProperty(ref _Value, value);
                    _ValueStr = value.ToString();
                    _DatasContext.Save();
                }
                else
                {
                    SetProperty(ref _Value, value);
                }

            }
        }
        private string _ValueStr;
        /// <summary>
        /// 值Str
        /// </summary>
        public string ValueStr
        {
            get { return _ValueStr; }
            set
            {

                _ValueStr = value;

            }
        }


        private dynamic _DefaultValue;
        /// <summary>
        /// 默认值
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 初始值
        /// </summary>
        public dynamic DefaultValue
        {
            get => _DefaultValue;
            set
            {
                if (!IsValidValue(value, Type)) return;
                SetProperty(ref _DefaultValue, value);
                if (_DefaultValue != null)
                {
                    _DefaultValueStr = _DefaultValue.ToString();
                }

            }
        }
        private string _DefaultValueStr;
        /// <summary>
        /// Default值Str
        /// </summary>
        public string DefaultValueStr
        {
            get { return _DefaultValueStr; }
            set
            {

                _DefaultValueStr = value;

            }
        }

        /// <summary> 
        /// 数据类型
        /// </summary>
        public VariableDataType Type { get; set; }

        /// <summary>
        /// RetainPersistent
        /// </summary>
        [Column(TypeName = "INTEGER")]
        /// <summary>
        /// 保持
        /// </summary>
        public bool RetainPersistent { get; set; }

        private static bool IsValidValue(dynamic value, VariableDataType type)
        {
            return type switch
            {
                VariableDataType.Int32 => int.TryParse(value.ToString(), out int _),
                VariableDataType.DOUBLE => double.TryParse(value.ToString(), out double _),
                VariableDataType.STRING => true,
                _ => false
            };
        }
        /// <summary>
        /// Dynamic值
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="type">类型</param>
        /// <returns>返回结果</returns>
        public static dynamic DynamicValue(string value, VariableDataType type)
        {
            dynamic result = value;
            switch (type)
            {
                case VariableDataType.Int32:
                    if (int.TryParse(value, out int intValue))
                    {
                        result = intValue;
                    }
                    break;
                case VariableDataType.DOUBLE:
                    if (double.TryParse(value, out double doubleValue))
                    {
                        result = doubleValue;
                    }
                    break;
                case VariableDataType.STRING:

                    break;
                default:
                    break;
            }
            return result;
        }
    }
    /// <summary>
    /// 系统全局变量
    /// </summary>
    public class SystemVariable : BindableBase
    {
        /// <summary>
        /// _DatasContext
        /// </summary>
        [NotMapped]
        public DatasContext _DatasContext { get; set; }
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();
        /// <summary>
        /// 配方标识
        /// </summary>
        public Guid RecipeID { get; set; }

        /// <summary>
        /// 变量名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Comment { get; set; }


        private dynamic _Value;
        /// <summary>
        /// 值
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 当前值
        /// </summary>
        public dynamic Value
        {
            get => _Value;
            set
            {
                if (!IsValidValue(value, Type)) return;


                if (RetainPersistent && value != null && _Value != value)
                {
                    SetProperty(ref _Value, value);
                    _ValueStr = value.ToString();
                    _DatasContext.Save();
                }
                else
                {
                    SetProperty(ref _Value, value);
                }

            }
        }
        private string _ValueStr;
        /// <summary>
        /// 值Str
        /// </summary>
        public string ValueStr
        {
            get { return _ValueStr; }
            set
            {

                _ValueStr = value;

            }
        }


        private dynamic _DefaultValue;
        /// <summary>
        /// 默认值
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 初始值
        /// </summary>
        public dynamic DefaultValue
        {
            get => _DefaultValue;
            set
            {
                if (!IsValidValue(value, Type)) return;
                SetProperty(ref _DefaultValue, value);
                if (_DefaultValue != null)
                {
                    _DefaultValueStr = _DefaultValue.ToString();
                }

            }
        }
        private string _DefaultValueStr;
        /// <summary>
        /// Default值Str
        /// </summary>
        public string DefaultValueStr
        {
            get { return _DefaultValueStr; }
            set
            {

                _DefaultValueStr = value;

            }
        }

        /// <summary> 
        /// 数据类型
        /// </summary>
        public VariableDataType Type { get; set; }

        /// <summary>
        /// RetainPersistent
        /// </summary>
        [Column(TypeName = "INTEGER")]
        /// <summary>
        /// 保持
        /// </summary>
        public bool RetainPersistent { get; set; }

        private static bool IsValidValue(dynamic value, VariableDataType type)
        {
            return type switch
            {
                VariableDataType.Int32 => int.TryParse(value.ToString(), out int _),
                VariableDataType.DOUBLE => double.TryParse(value.ToString(), out double _),
                VariableDataType.STRING => true,
                _ => false
            };
        }
        /// <summary>
        /// Dynamic值
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="type">类型</param>
        /// <returns>返回结果</returns>
        public static dynamic DynamicValue(string value, VariableDataType type)
        {
            dynamic result = value;
            switch (type)
            {
                case VariableDataType.Int32:
                    if (int.TryParse(value, out int intValue))
                    {
                        result = intValue;
                    }
                    break;
                case VariableDataType.DOUBLE:
                    if (double.TryParse(value, out double doubleValue))
                    {
                        result = doubleValue;
                    }
                    break;
                case VariableDataType.STRING:

                    break;
                default:
                    break;
            }
            return result;
        }
    }
}