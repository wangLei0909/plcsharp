using Prism.Mvvm;
using Prism.Navigation;

namespace PLCSharp.Core.Prism
{
    /// <summary>
    /// 视图模型Base
    /// </summary>
    public abstract class ViewModelBase : BindableBase, IDestructible
    {
        /// <summary>
        /// 视图模型Base
        /// </summary>
        protected ViewModelBase()
        {
        }

        /// <summary>
        /// Destroy
        /// </summary>
        public virtual void Destroy()
        {
        }
    }
}