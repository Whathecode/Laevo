using System;
using System.Collections.Generic;


namespace Laevo
{
    public interface IServiceLocator
    {
        T GetService<T>();
        void RegisterService<T>(object service);
        void RegisterService<T>(T service);
    }


    class ServiceLocator : IServiceLocator
    {
        static ServiceLocator _instace;
        private readonly Dictionary<Type, object> _services;

        public static ServiceLocator GetInstance()
        {
            return _instace ?? ( _instace = new ServiceLocator() );
        }

        private ServiceLocator()
        {
            _services = new Dictionary<Type, object>();
        }

        public void RegisterService<T>( object service )
        {
            _services.Add(typeof(T), (T) service);
        }

        public void RegisterService<T>(T service)
        {
            _services.Add(typeof(T), service);
        }

        public T GetService<T>()
        {
            try
            {
                return (T)_services[ typeof( T ) ];
            }
            catch ( KeyNotFoundException e )
            {
                throw new InvalidOperationException( "The service is not registered" );
            }
        }
    }
}
