# Automatic IDisposable Base Class Implementation

The **Disposable** class provides a robust implementation of the **IDisposable** pattern. It ensures that both managed and unmanaged resources are disposed of properly. It also automatically disposes properties and fields of the instance that implement IDisposable, making it a useful base class for objects that manage resources requiring explicit cleanup. To use this class, simply add *Disposable.cs* to your project. The class is implemented to support different versions of .NET, ensuring compatibility across various environments.

## Key Features
- **Automatic disposal:** The class automatically disposes of properties and fields that implement IDisposable when the Dispose method is called.
- **Customizable exception handling:** The class provides an option to ignore exceptions during the disposal process.
- **Dispose resources efficiently:** All resources are disposed of in an orderly manner, reducing the risk of resource leaks. The class is especially useful for automating resource management in objects that hold many resources that must be released after the object is deleted.

## Usage
You can use this class as a base class for your own objects that manage resources. Here's how to implement and use it:

Example: Using the Disposable class
```csharp
public class MyResource : Disposable
{
    public IDisposable Resource { get; set; }

    public MyResource() 
        : base(ignoreExceptions: false)
    {
        Resource = new SomeDisposableResource();
    }

    // Optionally, override Dispose to clean up any custom resources.
    protected override void ClearManagedResources()
    {
        // Custom cleanup code if needed.
    }

    protected override void ClearUnmanagedResources()
    {
        // Custom unmanaged resource cleanup code if needed.
    }
}

class Program
{
    static void Main()
    {
        using (var myResource = new MyResource())
        {
            // Use myResource here.
        }
    }
}
```

## Constructor & Property
**Constructor:** The constructor allows you to specify whether exceptions thrown during the disposal process should be ignored. By default, exceptions are thrown if disposal fails.  
**IgnoreExceptions:** This public property lets you modify the exception handling behavior at runtime, toggling whether exceptions will be ignored or not during disposal.  

## Advantages
1. **Automatic disposal:** Automatically handles the disposal of all IDisposable properties and fields, ensuring that resources are properly cleaned up.
2. **Exception handling flexibility:** The ability to ignore exceptions during disposal can be useful in scenarios where cleanup errors should not disrupt the program flow.
3. **Prevents resource leaks:** By ensuring that every IDisposable object is disposed of, this class reduces the risk of resource leaks.
4. **Easy integration:** You can easily extend it by inheriting from Disposable and overriding the cleanup methods (**ClearManagedResources** and **ClearUnmanagedResources**).
5. **Cross-version support:** The class is implemented to work with different versions of the .NET Framework, making it versatile for various .NET applications.

## Drawbacks
1. **Potential overhead:** The automatic disposal of all IDisposable fields and properties may introduce performance overhead, especially if the class has many such fields/properties.
2. **Misuse:** If you forget to call Dispose() in your code, unmanaged resources might not be released, leading to memory leaks. It is important to always use the class within a using block or ensure that Dispose() is called explicitly.
3. **Hidden exceptions:** By choosing to ignore exceptions during disposal (**IgnoreExceptionsInCatch** = true), you may miss critical errors that could indicate problems in your resources or disposal logic.
## When to Use
- Recommended: Use this class when you have objects that manage multiple resources and need to ensure that all IDisposable properties and fields are disposed of automatically. It is especially useful when dealing with complex objects that may require extensive cleanup, or when automating resource management in objects that hold many resources.
- Not recommended: If your object does not manage disposable resources or if you want fine-grained control over the disposal of each resource, it might be better to implement the IDisposable pattern manually for each resource.

## Conclusion
The Disposable class provides a powerful and convenient way to manage resources and ensure proper cleanup, following the **IDisposable** pattern. It simplifies the implementation of resource cleanup by automatically disposing of all disposable fields and properties. However, it should be used carefully to avoid hidden exceptions and unnecessary overhead in certain cases.

If you are developing applications that manage external resources or memory-intensive objects, the **Disposable** base class can help you write cleaner, safer code, and can be easily integrated into your project by adding Disposable.cs.
