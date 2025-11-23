using System;
using System.Collections.Generic;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy
{
    public static partial class DataItemFeatureSetup<T>
    {
        public static Func<T, T, T> SumFuncRepository;
        public static Func<T, T, T> MaxFuncRepository;
        public static Func<T, T> IncrementFuncRepository;
        public static Func<T, int, double> DivByIntFuncRepository;
        
        internal static Func<IRandomAccessTransactionData<T, T>, IRandomAccessTransactionData<T, T>> CopyFunc { get; set; } = (s) => s.Copy();
        internal static Func<IRandomAccessTransactionData<T, T>, IRandomAccessTransactionData<T, T>> CloneFunc { get; set; } = (s) => s.Clone();
        internal static Func<TableStorageType, TableStorageTypeModifier, T, T, bool> EqualsFunc { get; set; } 
        internal static Func<IRandomAccessData<T>, T,  Func<TableStorageType, TableStorageTypeModifier, T, T, bool>, IEnumerable<int>> FilterFunc { get; set; } 
        internal static Func<IRandomAccessData<T>, IEnumerable<int>, AggregateType, object> AggregateFunc { get; set; }
        
        public static void SetCloneFeature(Delegate @delegate) => CloneFunc = (Func<IRandomAccessTransactionData<T, T>, IRandomAccessTransactionData<T, T>>)@delegate;
        public static void SetCopyFeature(Delegate @delegate) => CopyFunc = (Func<IRandomAccessTransactionData<T, T>, IRandomAccessTransactionData<T, T>>)@delegate;
        public static void SetEqualsFeature(Delegate @delegate) => EqualsFunc = (Func<TableStorageType, TableStorageTypeModifier, T, T, bool>)@delegate;
        public static void SetFilterFeature(Delegate @delegate) => FilterFunc = (Func<IRandomAccessData<T>, T, Func<TableStorageType, TableStorageTypeModifier, T, T, bool>, IEnumerable<int>>)@delegate;
        public static void SetAggregateFeature(Delegate @delegate) => AggregateFunc = (Func<IRandomAccessData<T>, IEnumerable<int>, AggregateType, object>)@delegate;
        public static void SetEqualityComparer(Delegate @delegate) => AggregateFunc = (Func<IRandomAccessData<T>, IEnumerable<int>, AggregateType, object>)@delegate;
    }
}