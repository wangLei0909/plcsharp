using Newtonsoft.Json;
using Prism.Mvvm;

namespace PLCSharp.VVMs.Connects.ModbusTcp
{
    /// <summary>
    /// Modbus 服务端可访问地址条目
    /// </summary>
    public class DataItem : BindableBase
    {
        public enum ValueInterpretationEnum
        {
            UInt16,
            /// <summary>两个连续寄存器组成 32 位有符号整数 (大端)</summary>
            Int32,
            /// <summary>两个连续寄存器组成 32 位 IEEE 754 浮点数 (大端)</summary>
            Float,
            /// <summary>每个寄存器存 2 个字符 (高字节在前)</summary>
            String,
        }

        private ushort _Address;
        /// <summary>
        /// Address
        /// </summary>
        public ushort Address
        {
            get => _Address;
            set => SetProperty(ref _Address, value);
        }

        private ushort _Value;
        /// <summary>
        /// 值
        /// </summary>
        public ushort Value
        {
            get => _Value;
            set
            {
                SetProperty(ref _Value, value);
                RefreshDisplay();
            }
        }

        private ushort _HighValue;
        /// <summary>
        /// High值
        /// </summary>
        public ushort HighValue
        {
            get => _HighValue;
            set
            {
                SetProperty(ref _HighValue, value);

            }
        }

        private ValueInterpretationEnum _ValueInterpretation = ValueInterpretationEnum.UInt16;
        /// <summary>
        /// 值Interpretation
        /// </summary>
        public ValueInterpretationEnum ValueInterpretation
        {
            get => _ValueInterpretation;
            set
            {
                SetProperty(ref _ValueInterpretation, value);
                RefreshDisplay();
            }
        }

        // ───────── 显示值（只读，由 RefreshDisplay 更新） ─────────
        private string _DisplayValue = "0";
        /// <summary>
        /// Display值
        /// </summary>
        [JsonIgnore]
        public string DisplayValue
        {
            get => _DisplayValue;
            private set => SetProperty(ref _DisplayValue, value);
        }

        // ───────── 本地设定值（服务端：输入即解析更新当前值） ─────────
        private string _InputValue;
        /// <summary>
        /// Input值
        /// </summary>
        public string InputValue
        {
            get => _InputValue;
            set
            {
                SetProperty(ref _InputValue, value);
                ParseInputValueAndRefresh();
            }
        }
        private byte _UnitId = 1;
        /// <summary>
        /// UnitId
        /// </summary>
        public byte UnitId
        {
            get { return _UnitId; }
            set { SetProperty(ref _UnitId, value); }
        }
        // ───────── 远程发送值（客户端：输入不更新当前值，发送时解析） ─────────
        private string _SendValue = "";
        /// <summary>
        /// 发送值
        /// </summary>
        public string SendValue { get => _SendValue; set => SetProperty(ref _SendValue, value); }

        private string _Description = "";
        /// <summary>
        /// Description
        /// </summary>
        public string Description
        {
            get => _Description;
            set => SetProperty(ref _Description, value);
        }

        private void RefreshDisplay()
        {
            DisplayValue = ValueInterpretation switch
            {
                ValueInterpretationEnum.UInt16 => Value.ToString(),
                ValueInterpretationEnum.Int32 => CombineToInt32(Value, HighValue).ToString(),
                ValueInterpretationEnum.Float => CombineToFloat(Value, HighValue).ToString("R"),
                ValueInterpretationEnum.String => DecodeString(Value, HighValue),
                _ => Value.ToString(),
            };
        }

        /// <summary>解析 InputValue 到 _Value / _HighValue（发送前调用）</summary>
        public void ParseInputValue()
        {
            if (string.IsNullOrEmpty(_InputValue)) return;

            switch (ValueInterpretation)
            {
                case ValueInterpretationEnum.UInt16:
                    if (ushort.TryParse(_InputValue, out ushort uv))
                        _Value = uv;
                    break;

                case ValueInterpretationEnum.Int32:
                    if (int.TryParse(_InputValue, out int iv))
                    {
                        uint u = (uint)iv;
                        _Value = (ushort)(u & 0xFFFF);
                        _HighValue = (ushort)((u >> 16) & 0xFFFF);
                    }
                    break;

                case ValueInterpretationEnum.Float:
                    if (float.TryParse(_InputValue, out float fv))
                    {
                        uint raw = BitConverter.ToUInt32(BitConverter.GetBytes(fv));
                        _HighValue = (ushort)((raw >> 16) & 0xFFFF);
                        _Value = (ushort)(raw & 0xFFFF);
                    }
                    break;

                case ValueInterpretationEnum.String:
                    {
                        string s = _InputValue ?? "";
                        char c1 = s.Length > 0 ? s[0] : '\0';
                        char c2 = s.Length > 1 ? s[1] : '\0';
                        char c3 = s.Length > 2 ? s[2] : '\0';
                        char c4 = s.Length > 3 ? s[3] : '\0';
                        _Value = (ushort)((c1 << 8) | c2);
                        _HighValue = (ushort)((c3 << 8) | c4);
                    }
                    break;
            }
        }

        /// <summary>解析 InputValue 并刷新 DisplayValue（服务端输入时调用）</summary>
        public void ParseInputValueAndRefresh()
        {
            ParseInputValue();
            RefreshDisplay();
        }

        /// <summary>解析 SendValue 到 _Value / _HighValue（发送前调用，不刷新 DisplayValue）</summary>
        public void ParseSendValue()
        {
            if (string.IsNullOrEmpty(_SendValue)) return;
            string oldInput = _InputValue;
            _InputValue = _SendValue;
            ParseInputValue();
            _InputValue = oldInput;
        }

        private static int CombineToInt32(ushort lo, ushort hi) => (hi << 16) | lo;

        private static float CombineToFloat(ushort lo, ushort hi)
        {
            uint raw = (uint)((hi << 16) | lo);
            return BitConverter.ToSingle(BitConverter.GetBytes(raw));
        }

        private static string DecodeString(ushort lo, ushort hi)
        {
            char c1 = (char)((lo >> 8) & 0xFF);
            char c2 = (char)(lo & 0xFF);
            char c3 = (char)((hi >> 8) & 0xFF);
            char c4 = (char)(hi & 0xFF);
            return $"{c1}{c2}{c3}{c4}";
        }

        /// <summary>从服务端读取更新时直接设置原始值（仅刷新显示，不改 InputValue，不触发写入）</summary>
        public void SetRawValues(ushort lo, ushort hi)
        {
            _Value = lo;
            _HighValue = hi;
            RefreshDisplay();
        }
    }
}
