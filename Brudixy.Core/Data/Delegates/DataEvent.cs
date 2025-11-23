using System;
using System.Diagnostics;
using System.Linq;
using Brudixy.Interfaces.Delegates;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy.Delegates
{
    public static class DataEventFactory
    {
        public static TargetDataEvent<T> CreateAttachedEvent<T>()
        {
            return new TargetDataEvent<T>();
        }
        
        public static DataEvent<T> CreateDetachedEvent<T>([NotNull] IDisposableCollection referenceHolder)
        {
            return new DataEvent<T>(referenceHolder);
        }
    }

    public class DataEvent<T> : TargetDataEvent<T> , IDataEvent<T>
    {
        private IDisposableCollection m_referenceHolder;

        public DataEvent([NotNull] IDisposableCollection referenceHolder)
        {
            m_referenceHolder = referenceHolder ?? throw new ArgumentNullException(nameof(referenceHolder));
            m_receivers = new Data<WeakReference>();
        }
        
        private class ActionEventLambda<T> : IDataEventReceiver<T>, IDisposable
        {
            public Action<T, string> Delagate;

            public ActionEventLambda(Action<T, string> delagate)
            {
                Delagate = delagate;
            }

            public bool OnEvent(T args, string context = null)
            {
                Delagate?.Invoke(args, context);

                return false;
            }

            public void Dispose() => Delagate = null;
        }
        
        private class FuncEventLambda<T> : IDataEventReceiver<T>, IDisposable
        {
            public Func<T, string, bool> Delagate;

            public FuncEventLambda(Func<T, string, bool> delagate)
            {
                Delagate = delagate;
            }

            public bool OnEvent(T args, string context = null)
            {
                return Delagate?.Invoke(args, context) ?? false;
            }

            public void Dispose() => Delagate = null;
        }

        public void Subscribe([NotNull] Action<T, string> action, string context = null)
        {
            if (ReferenceEquals(null, action))
            {
                throw new ArgumentNullException(nameof(action));
            }

            var eventLambda = new ActionEventLambda<T>(action);

            SubscribeTarget(eventLambda, context);

            m_referenceHolder.AddDisposable(eventLambda);
        }
        
        public void Subscribe([NotNull] Func<T, string, bool> action, string context = null)
        {
            if (ReferenceEquals(null, action))
            {
                throw new ArgumentNullException(nameof(action));
            }

            var eventLambda = new FuncEventLambda<T>(action);

            SubscribeTarget(eventLambda, context);

            m_referenceHolder.AddDisposable(eventLambda);
        }

        public void Unsubscribe([NotNull] Action<T, string> action)
        {
            if (ReferenceEquals(null, action))
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (m_referenceHolder == null)
            {
                return;
            }
            
            var eventLambdas = m_referenceHolder
                .Items
                .OfType<ActionEventLambda<T>>()
                .Where(eventLambda => ReferenceEquals(eventLambda.Delagate, action))
                .ToArray();

            foreach (var eventLambda in eventLambdas)
            {
                UnsubscribeTarget(eventLambda);

                m_referenceHolder.RemoveDisposable(eventLambda);
            }
        }
        
        public void Unsubscribe([NotNull] Func<T, string, bool> action)
        {
            if (ReferenceEquals(null, action))
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (m_referenceHolder == null)
            {
                return;
            }
            
            var eventLambdas = m_referenceHolder
                .Items
                .OfType<FuncEventLambda<T>>()
                .Where(eventLambda => ReferenceEquals(eventLambda.Delagate, action))
                .ToArray();

            foreach (var eventLambda in eventLambdas)
            {
                UnsubscribeTarget(eventLambda);

                m_referenceHolder.RemoveDisposable(eventLambda);
            }
        }
        
        public IDataEvent<T> Clone(IDisposableCollection referenceHolder)
        {
            if (m_receivers == null)
            {
                throw new ObjectDisposedException("DataEvent is disposed");
            }
            
            var clone = (DataEvent<T>)this.MemberwiseClone();

            clone.m_referenceHolder = referenceHolder;

            lock (this)
            {
                clone.m_receivers = new Data<WeakReference>(m_receivers.Count);

                foreach (var weakReference in m_receivers)
                {
                    if (weakReference.IsAlive)
                    {
                        var referenceTarget = weakReference.Target;
                        
                        clone.m_receivers.Add(new WeakReference(referenceTarget));
                        
                        if (referenceTarget is IDisposable disposable)
                        {
                            clone.m_referenceHolder.AddDisposable(disposable);
                        }  
                    }
                }
            }

            return clone;
        }

        public override void Dispose()
        {
            base.Dispose();

            m_referenceHolder = null;
        }
    }
    
    public class TargetDataEvent<T> : ITargetDataEvent<T>, IDisposable
    {
        protected Data<WeakReference> m_receivers;
        
        public TargetDataEvent()
        {
            m_receivers = new Data<WeakReference>();
        }

        public void CopyTo(TargetDataEvent<T> target)
        {
            if (m_receivers == null)
            {
                throw new ObjectDisposedException("DataEvent is disposed.");
            }
            
            lock (this)
            {
                if (m_receivers == null)
                {
                    throw new ObjectDisposedException("DataEvent is disposed");
                }
                
                foreach (var receiver in this.m_receivers.ToArray())
                {
                    if (receiver.IsAlive)
                    {
                        var receiverTarget = receiver.Target;
                        
                        target.m_receivers.Add(new WeakReference(receiverTarget));
                    }
                }
            }
        }

        public bool HasAny()
        {
            lock (this)
            {
                return m_receivers?.Count > 0;
            }
        }
        
        private sealed class Target
        {
            public IDataEventReceiver<T> Receiver { get; }
            public string Context { get; }

            public Target(IDataEventReceiver<T> receiver, string context)
            {
                Receiver = receiver;
                Context = context;
            }
        }

        public void SubscribeTarget([NotNull] IDataEventReceiver<T> target, string context = null)
        {
            if (ReferenceEquals(null, target))
            {
                throw new ArgumentNullException(nameof(target));
            }

            lock (this)
            {
                if (m_receivers == null)
                {
                    throw new ObjectDisposedException("DataEvent is disposed.");
                }
                
                m_receivers.Add(new WeakReference(new Target(target, context)));
            }
        }

        public void UnsubscribeTarget([NotNull] IDataEventReceiver<T> target)
        {
            if (ReferenceEquals(null, target))
            {
                throw new ArgumentNullException(nameof(target));
            }

            lock (this)
            {
                if (m_receivers == null)
                {
                    return;
                }
             
                m_receivers.RemoveAll(target, (receiver) => (receiver.Target as Target)?.Receiver);
            }
        }

        public void Raise([NotNull] T args)
        {
            if (ReferenceEquals(null, args))
            {
                throw new ArgumentNullException(nameof(args));
            }

            lock (this)
            {
                if (m_receivers == null)
                {
                    return;
                }
                
                List<int> deadReferences = null;

                var receivers = m_receivers.ToArray();

                try
                {
                    for (var index = receivers.Length - 1; index >= 0; index--)
                    {
                        var receiver = receivers[index];

                        var target = receiver.Target as Target;
                        
                        try
                        {

                            if (ReferenceEquals(null, target))
                            {
                                if (deadReferences == null)
                                {
                                    deadReferences = new List<int >();
                                }

                                //garbage collected item.
                                deadReferences.Add(index);
                                continue;
                            }

                            if (target.Receiver.OnEvent(args, target.Context))
                            {
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (Trace.Listeners.Count > 0)
                            {
                                Trace.WriteLine($"DataEvent<{typeof(T).Name}> handler '{target?.Context}' unhandled exception: {ex}.");
                            }

                            //do not suppress exception
                            throw;
                        }
                    }
                }
                finally
                {
                    //cleanup garbage  collected references.
                    if (deadReferences != null)
                    {
                        foreach (var index in deadReferences)
                        {
                            m_receivers.RemoveAt(index);
                        }
                    }
                }
            }
        }

        public virtual void Dispose()
        {
            lock (this)
            {
                m_receivers?.Dispose();
                
                m_receivers = null;
            }
        }
    }
}
