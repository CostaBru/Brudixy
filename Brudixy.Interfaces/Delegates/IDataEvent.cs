using System;
using JetBrains.Annotations;

namespace Brudixy.Interfaces.Delegates
{
    public interface IDataEvent<T> : ITargetDataEvent<T>
    {
        void Subscribe([NotNull] Action<T, string> action, string context = null);
        void Subscribe([NotNull] Func<T, string, bool> action, string context = null);

        void Unsubscribe([NotNull] Action<T, string> action);
        void Unsubscribe([NotNull] Func<T, string, bool> action);
    }
    
    public interface ITargetDataEvent<T>
    {
        void SubscribeTarget([NotNull] IDataEventReceiver<T> target, string context = null);

        void UnsubscribeTarget([NotNull] IDataEventReceiver<T> target);
    }
}