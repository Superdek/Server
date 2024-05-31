﻿# Server


## Code Conventions

### Types

The following list is available value types.

* bool, sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double

The following list is available reference types.

* object, string



#### Using Float & Double

Always use the F suffix for float literals and the D suffix for double literals to explicitly denote the type.

```c#
float floatValue = 3.14F;
double doubleValue = 3.14D;
```

### Exceptions

In case of exceptions that must be thrown, they must be documented using XML tags on the methods,
except the exception "System.NotImplementedException()".
All exceptions must be handled without exception, except for the exception 'System.NotImplementedException()', 
which should not be handled as users need to be notified for confirmation.

Throwing exceptions is not allowed except for network-related functions, but the exception "System.NotImplementedException()" can be used anywhere.
This means that any network operations that may potentially throw exceptions should be handled appropriately, 
while exceptions should not be used for other purposes within the codebase.

Throwing exceptions are restricted to network-related functions due to several reasons. 
First, network conditions are often unpredictable, 
and errors like connection timeouts or server unavailability can occur unexpectedly. 
By allowing exceptions specifically for network operations, developers can handle these unforeseen circumstances more effectively.
Furthermore, excessive and indiscriminate use of exceptions can significantly impact performance, especially during debugging. 
When exceptions are thrown frequently outside of network-related contexts, 
it becomes more challenging to monitor and debug code effectively. 
Focusing exceptions on network operations helps developers to isolate and address issues more efficiently without sacrificing overall performance.
Overall, restricting exceptions to network-related functions enhances code clarity, improves performance, and allows for better management of unpredictable network conditions.

### Containers
All containers must be implemented as IDisposable interface, and have empty data if the container is disposed.

### Dispose Pattern

```C#
class Base : IDisposable
{
    ...

	private bool _disposed = false;

    ...

	~Base() => Dispose(false);

    ...

    public void DoWork()
    {
        Debug.Assert(!_disposed);

        ...
    }

    ...

	protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
             
        // Assertion.

        if (disposing == true)
        {
            // Release managed resources.
        }

        // Release unmanaged resources.

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}

class Derived : Base
{
    ...

    private bool _disposed = false;
    
    ...
    
    ~Derived() => Dispose(false):

    ...

    public void DoWork2()
    {
        Debug.Assert(!_disposed);

        ...
    }

    ...

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            // Assertion.

            if (disposing == true)
            {
                // Release managed resources.
            }

            // Release unmanaged resources.

            _disposed = true;
        }

        base.Dispose(disposing);
    }

}

```

```C#
public sealed class Object : IDisposable
{
    private bool _disposed = false;

    ~Object() => Dispose(false);

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        // Assertion.

        if (disposing == true)
        {
            // Release managed resources.
        }

        // Release unmanaged resources.

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}
```

#### Disposable Instances

Disposable instances must be handled by 'using' statement except members of class and struct.
If disposable objects were used as members of class and struct, they must be disposed at the current object Dispose(bool disposing) fucntion.

