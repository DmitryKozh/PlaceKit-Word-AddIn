using System;
using System.Threading;
using PlaceKit.Models;

namespace PlaceKit.Interfaces
{
    public interface IProgressService
    {
        T Run<T>(string title, Func<IProgress<ProgressInfo>, CancellationToken, T> operation);
    }
}
