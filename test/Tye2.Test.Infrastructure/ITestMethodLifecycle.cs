
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
namespace Tye2.Test.Infrastructure
{
    /// <summary>
    /// Defines a lifecycle for attributes or classes that want to know about tests starting
    /// or ending. Implement this on a test class, or attribute at the method/class/assembly level.
    /// </summary>
    public interface ITestMethodLifecycle
    {
        Task OnTestStartAsync(TestContext context, CancellationToken cancellationToken);

        Task OnTestEndAsync(TestContext context, Exception exception, CancellationToken cancellationToken);
    }
}
