#if UNITY_EDITOR
using System;
using UnityEngine;


public class TestClass : IDisposable
{
    private bool disposed = false;

    // 관리되지 않는 리소스를 나타내는 예시 필드
    private IntPtr unmanagedResource;

    public TestClass()
    {
        // 100바이트의 관리되지 않는 메모리 할당
        unmanagedResource = System.Runtime.InteropServices.Marshal.AllocHGlobal(100);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // 관리되는 리소스 해제
            }

            // 관리되지 않는 리소스 해제
            if (unmanagedResource != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(unmanagedResource);
                unmanagedResource = IntPtr.Zero;
            }

            disposed = true;
        }
    }

    ~TestClass()
    {
        Dispose(false);
    }
}



public class ManualMemoryControl
{
    public void TestMethod()
    {
        using (TestClass obj = new TestClass())
        {
            // obj 사용
        } // using 블록을 벗어나면 obj.Dispose()가 자동으로 호출됩니다.
    }
}
#endif
