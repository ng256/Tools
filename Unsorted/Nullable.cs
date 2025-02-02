namespace System
{
    // A generic nullable class that encapsulates a nullable value type.
    // Provides the ability to work with nullable structures similarly to C#'s built-in nullable types.
    class Nullable<T> where T : struct
    {
        // A private, read-only field that stores the nullable value.
        private readonly T? _value;

        // Initializes the nullable object with a specified value.
        public Nullable(T? value)
        {
            _value = value;
        }

        // Initializes the nullable object with null.
        public Nullable()
        {
            _value = null;
        }

        // Implicit conversion from Nullable<T> to T.
        // If the nullable value is set, returns its value; otherwise, returns the default value for the type T.
        public static implicit operator T(Nullable<T> value)
        {
            return value?._value ?? default;
        }

        // Implicit conversion from T to Nullable<T>.
        // Allows a value of type T to be assigned directly to a Nullable<T> variable.
        public static implicit operator Nullable<T>(T value)
        {
            return new Nullable<T>(value);
        }
    }
}
