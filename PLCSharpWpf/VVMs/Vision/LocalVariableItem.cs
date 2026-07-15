using Newtonsoft.Json;
using Prism.Mvvm;

namespace PLCSharp.VVMs.Vision
{
    /// <summary>
    /// 局部变量表中的一条记录，保存在 <see cref="VisionParams.Variables"/> 中。
    /// </summary>
    public class LocalVariableItem : BindableBase
    {
        private string _Name;
        /// <summary>
        /// 变量名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }

        private string _VarType;
        /// <summary>
        /// 变量类型：Point / Line / Circle / Rect / Double
        /// </summary>
        public string VarType
        {
            get { return _VarType; }
            set { SetProperty(ref _VarType, value); }
        }

        private string _StrValue;
        [JsonIgnore]
        public string StrValue
        {
            get { return _StrValue; }
            set { SetProperty(ref _StrValue, value); }
        }

        private object? _RawValue;
        /// <summary>
        /// 原始值
        /// </summary>
        public object? RawValue
        {
            get { return _RawValue; }
            set
            {
                try
                {
                    switch (_VarType)
                    {
                        case "Pos":
                            {
                                var r = new Pos();
                                if (value is Pos pos)
                                {
                                    r = pos;
                                }
                                else
                                {
                                    r = JsonConvert.DeserializeObject<Pos>(value.ToString());
                                }
                                SetProperty(ref _RawValue, r);
                            }
                            break;
                        case "Line":
                            {
                                var r = new Line();
                                if (value is Line line)
                                {
                                    r = line;
                                }
                                else
                                {
                                    r = JsonConvert.DeserializeObject<Line>(value.ToString());
                                }
                                SetProperty(ref _RawValue, r);
                            }
                            break;
                        case "Circle":
                            {
                                var r = new Circle();
                                if (value is Circle line)
                                {
                                    r = line;
                                }
                                else
                                {
                                    r = JsonConvert.DeserializeObject<Circle>(value.ToString());
                                }
                                SetProperty(ref _RawValue, r);
                            }
                            break;
                        case "Rect":
                            {
                                var r = new Rect();
                                if (value is Rect rect)
                                {
                                    r = rect;
                                }
                                else
                                {
                                    r = JsonConvert.DeserializeObject<Rect>(value.ToString());
                                }

                                SetProperty(ref _RawValue, r);
                            }
                            break;
                        case "Double":
                            SetProperty(ref _RawValue, value);
                            break;
                        case "Barcode":
                            {
                                var r = new Barcode();
                                if (value is Barcode barcode)
                                {
                                    r = barcode;
                                }
                                else
                                {
                                    r = JsonConvert.DeserializeObject<Barcode>(value.ToString());
                                }

                                SetProperty(ref _RawValue, r);
                            }
                            break;


                    }

                    StrValue = FormatValue(_RawValue, _VarType);
                }
                catch (Exception)
                {

                  
                }
   
            }
        }

        /// <summary>
        /// 默认构造函数（用于 JSON 反序列化）
        /// </summary>
        public LocalVariableItem() { }

        /// <summary>
        /// 用指定名称、类型和初始值构造变量项，自动生成显示文本
        /// </summary>
        /// <param name="name">变量名称</param>
        /// <param name="varType">变量类型（Point/Line/Circle/Rect/Double）</param>
        /// <param name="rawValue">初始值</param>
        public LocalVariableItem(string name, string varType, object? rawValue)
        {
            _Name = name;
            _VarType = varType;
            RawValue = rawValue;

        }


        private static string FormatValue(object? val, string type)
        {
            if (val == null) return "null";
            try
            {

                switch (type)
                {
                    case "Pos":
                        if(val is Pos pt)
                        return $"位置(X:{pt.X:F4}, Y:{pt.Y:F4}), 角度:（{pt.Angle:F4})";
                        else
                            return val?.ToString() ?? "";
                       
                    case "Line":
                        if (val is Line line)
                            return $"(X:{line.From.X:F4}, Y:{line.From.Y:F4}) -> (X:{line.To.X:F4}, Y:{line.To.Y:F4})";
                        else
                            return val?.ToString() ?? "";
                      
                    case "Circle":
                        if (val is Circle c)
                            return $"中心(X:{c.Center.X:F4} Y:{c.Center.Y:F4}) 半径=({c.Radius:F4})";
                        else
                            return val?.ToString() ?? "";
                       
                    case "Rect":
                        if (val is Rect r)
                            return $"中心：(X:{r.Center.X:F4}, Y:{r.Center.Y:F4}) 宽高:({r.Width:F4} × {r.Height:F4}) 角度:（{r.Center.Angle:F4})";
                        else
                            return val?.ToString() ?? "";
                        
                    case "Double":
                        return ((double)val).ToString("F4");

                    case "Barcode":
                        if (val is Barcode b)
                            return $"位置：(X:{b.Box.X:F4}, Y:{b.Box.Y:F4}) 内容:（{b.Info})";
                        else
                            return val?.ToString() ?? "";

                    default:
                        return val?.ToString() ?? "";
                }
            }
            catch (Exception)
            {
                return val?.ToString() ?? "";
            }
        }
    }
}
