#nullable enable
using System.Collections.Specialized;
using System.ComponentModel;

namespace PLCSharp.Core.Prism
{
    /// <summary>
    /// 支持 WPF 绑定通知的可观察字典。
    /// 所有修改操作都会触发 <see cref="INotifyCollectionChanged"/> 和 <see cref="INotifyPropertyChanged"/> 事件，
    /// 使得 WPF 索引器绑定（如 <c>{Binding Dict["key"]}</c>）能自动更新 UI。
    /// </summary>
    public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
        where TKey : notnull
    {
        /// <summary>
        /// Observable字典
        /// </summary>
        public ObservableDictionary() : base()
        {
        }

        /// <summary>
        /// 从现有键值对集合构建，同时保留通知能力。
        /// 用于 JSON 反序列化后将普通 Dictionary 重新包装为 ObservableDictionary。
        /// </summary>
        public ObservableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : base()
        {
            foreach (var kvp in collection)
            {
                base.Add(kvp.Key, kvp.Value);
            }
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        #region 遮蔽基类成员以拦截修改

        /// <summary>
        /// Keys
        /// </summary>
        public new KeyCollection Keys => base.Keys;
        /// <summary>
        /// Values
        /// </summary>
        public new ValueCollection Values => base.Values;
        /// <summary>
        /// 数量
        /// </summary>
        public new int Count => base.Count;

        /// <summary>
        /// this[]
        /// </summary>
        /// <param name="key">key</param>
        public new TValue this[TKey key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">值</param>
        public new void Add(TKey key, TValue value)
        {
            if (ContainsKey(key))
            {
                var oldPair = CreatePair(key, base[key]);
                base[key] = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, CreatePair(key, value), oldPair));
            }
            else
            {
                base.Add(key, value);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, CreatePair(key, value)));
            }
        }

        /// <summary>
        /// 清空
        /// </summary>
        public new void Clear()
        {
            base.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>返回布尔值</returns>
        public new bool Remove(TKey key)
        {
            if (!ContainsKey(key)) return false;
            var pair = CreatePair(key, base[key]);
            if (base.Remove(key))
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, pair));
                return true;
            }
            return false;
        }

        /// <summary>
        /// ContainsKey
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>返回布尔值</returns>
        public new bool ContainsKey(TKey key) => base.ContainsKey(key);

        /// <summary>
        /// Try获取值
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">值</param>
        /// <returns>返回布尔值</returns>
        public new bool TryGetValue(TKey key, out TValue value) => base.TryGetValue(key, out value!);

        #endregion

        #region SetValue（被 indexer setter 调用）

        /// <summary>
        /// 设置值
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">值</param>
        public void SetValue(TKey key, TValue value)
        {
            if (ContainsKey(key))
            {
                var oldPair = CreatePair(key, base[key]);
                base[key] = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, CreatePair(key, value), oldPair));
            }
            else
            {
                base.Add(key, value);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, CreatePair(key, value)));
            }
        }

        #endregion

        #region 事件通知

        /// <summary>
        /// On集合Changed
        /// </summary>
        /// <param name="e">e</param>
        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            void Raise()
            {
                CollectionChanged?.Invoke(this, e);
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            }

            // 仅在非 UI 线程时 dispatch，避免不必要的一次异步排队
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
#pragma warning disable VSTHRD001 // 同步事件通知无法 async，与本项目其他 Dispatcher.Invoke 保持一致
                dispatcher.Invoke(Raise);
#pragma warning restore VSTHRD001
            }
            else
                Raise();
        }

        /// <summary>
        /// OnPropertyChanged
        /// </summary>
        /// <param name="e">e</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        #endregion

        #region 辅助方法

        private static KeyValuePair<TKey, TValue> CreatePair(TKey key, TValue value)
            => new(key, value);

        /// <summary>
        /// 根据 key 查找键值对。保留此公开方法以保证向后兼容，
        /// 但内部已不再使用（改用 O(1) 的 <see cref="CreatePair"/>）。
        /// </summary>
        [Obsolete("请直接通过索引器或 TryGetValue 访问；此方法 O(n) 遍历且不常用。")]
        public KeyValuePair<TKey, TValue> FindPair(TKey key)
        {
            foreach (var item in this)
            {
                if (item.Key.Equals(key))
                    return item;
            }
            return default;
        }

        /// <summary>
        /// 返回 key 在枚举顺序中的索引。保留以保证向后兼容。
        /// </summary>
        [Obsolete("字典无序，索引无实际意义。")]
        public int IndexOf(TKey key)
        {
            int index = 0;
            foreach (var item in this)
            {
                if (item.Key.Equals(key)) return index;
                index++;
            }
            return -1;
        }

        private TValue GetValue(TKey key)
        {
            return ContainsKey(key) ? base[key] : default!;
        }

        #endregion
    }
}
