using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace P1SerialToUdp
{
    public static class Extensions
    {
        public static IObservable<TSource> TakeUntil<TSource>(
            this IObservable<TSource> source, Func<TSource, bool> predicate)
        {
            return Observable
                .Create<TSource>(o => source.Subscribe(x =>
                {
                    o.OnNext(x);
                    if (predicate(x))
                        o.OnCompleted();
                },
                    o.OnError,
                    o.OnCompleted
                    ));
        }
        
        public static IObservable<byte> ToObservable(this SerialPort openPort)
        {
            return openPort.BaseStream.ToObservable();
        }

        public static async Task<byte[]> ReadAsync(this Stream stream, int bufSize = 1024)
        {
            var buffer = new byte[bufSize];
            var read = await stream.ReadAsync(buffer, 0, bufSize);
            return new ArraySegment<byte>(buffer, 0, read).ToArray();
        }

        public static IObservable<byte> ToObservable(this Stream stream)
        {
            return
                Observable.FromAsync(() => stream.ReadAsync())
                    .Repeat()
                    .TakeWhile(_ => _.Length != 0)
                    .SelectMany(arr => arr.ToObservable());
        }
    }
    /*
    public static class SignalRObservableExtensions
    {
        public static IDisposable PublishOnSignalR<T>(this IObservable<T> observable, IHubContext hub, string eventName)
        {
            return observable.Subscribe(
                value => RaiseOnNext(hub, eventName, value));
        }

        private static IHubConnectionContext Clients<THub>() where THub : Hub, new()
        {
            return GlobalHost.ConnectionManager.GetHubContext<THub>().Clients;
        }

        public static void RaiseOnNext<T>(IHubContext hub, string eventName, T payload)
        {
            dynamic clients = hub.Clients;
            var context = GlobalHost.ConnectionManager.GetHubContext<P1Hub>();
            context.Clients.Broadcast(new { Data = payload, EventName = eventName, Type = ClientsideConstants.OnNextType });
        }

        public static void RaiseOnNext<T>(string eventName, dynamic clients, T payload)
        {
            clients.subjectOnNext(new { Data = payload, EventName = eventName, Type = ClientsideConstants.OnNextType });
        }

        public static void RaiseOnError(string eventName, dynamic clients, Exception payload)
        {
            clients.subjectOnNext(new { Data = payload, EventName = eventName, Type = ClientsideConstants.OnErrorType });
        }

        public static void RaiseOnCompleted(string eventName, dynamic clients)
        {
            clients.subjectOnNext(new { EventName = eventName, Type = ClientsideConstants.OnCompletedType });
        }

        public static class ClientsideConstants
        {
            public const string OnNextType = "onNext";
            public const string OnErrorType = "onError";
            public const string OnCompletedType = "onCompleted";
        }
    }*/
}