﻿using System;
using REstate.Configuration;
using REstate.Configuration.Builder;
using REstate.Configuration.Builder.Implementation;
using REstate.Engine;
using REstate.Engine.Repositories.InMemory;
using REstate.Engine.Services;
using REstate.IoC;
using REstate.IoC.BoDi;

namespace REstate
{
    public static class REstateHost
    {
        static REstateHost()
        {
            Register(new BoDiComponentContainer(new ObjectContainer()));
        }

        private static IComponentContainer _container;

        public static IStateEngine<TState, TInput> GetStateEngine<TState, TInput>() =>
            _container.Resolve<IStateEngine<TState, TInput>>();

        public static string GetDiagram<TState, TInput>(this Schematic<TState, TInput> schematic)
        {
            return _container.Resolve<ICartographer<TState, TInput>>().WriteMap(schematic.StateConfigurations);
        }

        /// <summary>
        /// Register's defaults and the REstate engine in a given container.
        /// </summary>
        /// <param name="container">An adapter to an IoC/DI container.</param>
        public static void Register(IComponentContainer container)
        {
            container.Register(typeof(IConnectorResolver<,>), typeof(DefaultConnectorResolver<,>));

            container.Register(typeof(IStateMachineFactory<,>), typeof(REstateMachineFactory<,>));
            container.Register(typeof(IStateEngine<,>), typeof(StateEngine<,>));

            container.Register(typeof(ICartographer<,>), typeof(DotGraphCartographer<,>));

            container.RegisterComponent(new InMemoryRepositoryComponent());

            _container = container;

            RegisterConnector("Console", typeof(ConsoleConnector<,>));
        }

        public static void RegisterComponent(IComponent component) =>
            _container.RegisterComponent(component);

        public static void RegisterConnector(string connectorKey, Type connectorType) =>
            _container.Register(typeof(IConnector<,>), connectorType, connectorKey);

        public static ISchematicBuilder<TState, TInput> CreateSchematic<TState, TInput>(string schematicName) =>
            new SchematicBuilder<TState, TInput>(schematicName);
    }
}
