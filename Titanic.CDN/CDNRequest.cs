using System;
using System.Threading;

namespace Titanic.CDN
{
    public abstract class CDNRequest<T>
    {
        protected abstract T Execute(TitanicCDN cdn);
        public delegate void OnSuccess(T response);
        public delegate void OnError(Exception e);

        public void Perform(TitanicCDN cdn, OnSuccess? onSuccess, OnError? onError)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try {
                    T response = Execute(cdn);
                    onSuccess?.Invoke(response);
                } catch (Exception e) {
                    onError?.Invoke(e);
                }
            });
        }

        public void Perform(TitanicCDN cdn)
        {
            Perform(cdn, null, null);
        }

        public void Perform(TitanicCDN cdn, OnSuccess? onSuccess)
        {
            Perform(cdn, onSuccess, null);
        }

        public T BlockingPerform(TitanicCDN cdn)
        {
            return Execute(cdn);
        }
    }
}
