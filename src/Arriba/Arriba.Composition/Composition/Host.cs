// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Linq;
using Arriba.Communication;
using Arriba.Communication.ContentTypes;
using Arriba.Model;

namespace Arriba.Composition
{
    public class Host : IDisposable
    {
        private CompositionHost _container = null;
        private ContainerConfiguration _configuration = null;

        public Host()
        {
            ArribaServices.Initialize();
            var conventions = new ConventionBuilder();
            conventions.ForTypesDerivedFrom<IChannel>()
                .ExportInterfaces()
                .Shared();

            conventions.ForTypesDerivedFrom<IContentReader>()
                .Export<IContentReader>()
                .Shared();

            conventions.ForTypesDerivedFrom<IContentWriter>()
                .Export()
                .Export<IContentWriter>()
                .Shared();

            conventions.ForTypesDerivedFrom<IApplication>()
                .ExportInterfaces()
                .Shared();

            var assemblies = new[] {
                // Arriba.dll
                typeof(Table).Assembly, 
                // Arriba.Composition.dll
                typeof(Host).Assembly,
                // Arriba.Adapter.Newtonsoft
                typeof(JsonContentWriter).Assembly
            };

            _configuration = new ContainerConfiguration().WithAssemblies(assemblies.Distinct(), conventions);
        }

        public void Compose()
        {
            _container = _configuration.CreateContainer();
        }

        public void Add<TContract>(TContract value)
        {
            _configuration.WithExport<TContract>(value);
        }

        public void AddConfigurationValue<T>(string name, T value)
        {
            _configuration.WithExport<T>(value, contractName: name);
        }

        public T GetService<T>()
        {
            return _container.GetExport<T>();
        }

        public IEnumerable<T> GetServices<T>()
        {
            return _container.GetExports<T>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (_container != null)
            {
                _container.Dispose();
            }
        }
    }
}
