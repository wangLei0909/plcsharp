using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;

namespace PLCSharp.VVMs.Homepage.TableControl
{
    /// <summary>
    /// RangeTable视图模型
    /// </summary>
    public class RangeTableViewModel : BindableBase
    {
        private ObservableCollection<RangeValue> _RangeValues = [];
        /// <summary>
        /// 配置项
        /// </summary>
        public ObservableCollection<RangeValue> RangeValues
        {
            get => _RangeValues;
            set => SetProperty(ref _RangeValues, value);
        }

        /// <summary>
        /// 按名称查找 RangeValue
        /// </summary>
        public RangeValue? Find(string name)
        {
            return RangeValues.FirstOrDefault(r => r.Name == name);
        }

        /// <summary>
        /// 设置测量值并触发 OK/NG 判断
        /// </summary>
        public void SetMeasurement(string name, double value)
        {
            var rv = Find(name);
            if (rv != null)
            {
                rv.ShowValue = value;
                rv.GetResult();
            }
        }
    }
}
