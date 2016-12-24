namespace Miruken.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public static class PropertyChangedExtensions
    {
        public static bool ChangeProperty<T>(
            this PropertyChangedEventHandler propertyChanged,
            ref T field, T value, object sender, IEqualityComparer<T> comparer = null, 
            [CallerMemberName] string propertyName = null)
        {
            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            if (comparer.Equals(field, value))
                return false;

            field = value;
            propertyChanged?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public static bool ChangeProperty<T>(
             this EventHandler<PropertyChangedEventArgs> propertyChanged,
             ref T field, T value, object sender, IEqualityComparer<T> comparer = null,
             [CallerMemberName] string propertyName = null)
        {
            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            if (comparer.Equals(field, value))
                return false;

            field = value;
            propertyChanged?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
