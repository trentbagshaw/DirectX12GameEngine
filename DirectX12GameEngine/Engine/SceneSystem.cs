﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Engine
{
    public sealed class SceneSystem : GameSystem
    {
        private readonly HashSet<Entity> entities = new HashSet<Entity>();
        private readonly Dictionary<Type, List<EntitySystem>> systemsPerComponentType = new Dictionary<Type, List<EntitySystem>>();

        private Scene? rootScene;

        public SceneSystem(IServiceProvider services) : base(services)
        {
        }

        public EntitySystemCollection Systems { get; } = new EntitySystemCollection();

        public CameraComponent? CurrentCamera { get; set; }

        public string? InitialScenePath { get; set; }

        public Scene? RootScene
        {
            get => rootScene;
            set
            {
                if (rootScene == value) return;

                if (rootScene != null)
                {
                    Remove(rootScene);
                }

                if (value != null)
                {
                    Add(value);
                }

                rootScene = value;
            }
        }

        public override async Task LoadContentAsync()
        {
            if (InitialScenePath != null)
            {
                RootScene = await Content.LoadAsync<Scene>(InitialScenePath);
            }
        }

        public override void Update(TimeSpan deltaTime)
        {
            lock (Systems)
            {
                foreach (EntitySystem system in Systems)
                {
                    system.Update(deltaTime);
                }
            }
        }

        public override void Draw(TimeSpan deltaTime)
        {
            lock (Systems)
            {
                foreach (EntitySystem system in Systems)
                {
                    system.Draw(deltaTime);
                }
            }
        }

        public override void Dispose()
        {
            foreach (EntitySystem system in Systems)
            {
                system.Dispose();
            }
        }

        private void Add(Scene scene)
        {
            foreach (Entity entity in scene)
            {
                Add(entity);
            }

            scene.CollectionChanged += Entities_CollectionChanged;
        }

        private void Add(Entity entity)
        {
            if (!entities.Add(entity)) return;

            foreach (EntityComponent entityComponent in entity)
            {
                Add(entityComponent);
            }

            entity.CollectionChanged += Components_CollectionChanged;
        }

        private void Add(EntityComponent entityComponent)
        {
            CheckEntityComponentWithSystems(entityComponent, false);
        }

        private void Remove(Scene scene)
        {
            scene.CollectionChanged -= Entities_CollectionChanged;

            foreach (Entity entity in scene)
            {
                Remove(entity);
            }
        }

        private void Remove(Entity entity)
        {
            if (!entities.Remove(entity)) return;

            entity.CollectionChanged -= Components_CollectionChanged;

            foreach (EntityComponent entityComponent in entity)
            {
                Remove(entityComponent);
            }
        }

        private void Remove(EntityComponent entityComponent)
        {
            CheckEntityComponentWithSystems(entityComponent, true);
        }

        private void CheckEntityComponentWithSystems(EntityComponent entityComponent, bool forceRemove)
        {
            if (entityComponent.Entity is null) throw new ArgumentException("The entity component must be attached to an entity.", nameof(entityComponent));

            lock (Systems)
            {
                Type componentType = entityComponent.GetType();

                if (systemsPerComponentType.TryGetValue(componentType, out var systemsForComponent))
                {
                    foreach (EntitySystem system in systemsForComponent)
                    {
                        system.ProcessEntityComponent(entityComponent, forceRemove);
                    }
                }
                else
                {
                    if (!forceRemove)
                    {
                        CollectNewEntitySystems(componentType);
                    }

                    systemsForComponent = new List<EntitySystem>();

                    foreach (EntitySystem system in Systems)
                    {
                        if (system.Accept(componentType))
                        {
                            systemsForComponent.Add(system);
                            system.ProcessEntityComponent(entityComponent, forceRemove);
                        }
                    }

                    systemsPerComponentType.Add(componentType, systemsForComponent);
                }

                UpdateDependentSystems(entityComponent.Entity, entityComponent);
            }
        }

        private void CollectNewEntitySystems(Type componentType)
        {
            var entitySystemAttributes = componentType.GetCustomAttributes<DefaultEntitySystemAttribute>();

            foreach (DefaultEntitySystemAttribute entitySystemAttribute in entitySystemAttributes)
            {
                bool addNewSystem = !Systems.Exists(s => s.GetType() == entitySystemAttribute.Type);

                if (addNewSystem)
                {
                    EntitySystem system = (EntitySystem)Activator.CreateInstance(entitySystemAttribute.Type, Services);
                    Systems.Add(system);

                    Systems.Sort(EntitySystemCollection.EntitySystemComparer.Default);
                }
            }
        }

        private void UpdateDependentSystems(Entity entity, EntityComponent skipComponent)
        {
            foreach (EntityComponent entityComponent in entity.Components)
            {
                if (entityComponent == skipComponent) continue;

                Type componentType = entityComponent.GetType();

                if (systemsPerComponentType.TryGetValue(componentType, out var systemsForComponent))
                {
                    foreach (EntitySystem system in systemsForComponent)
                    {
                        system.ProcessEntityComponent(entityComponent, false);
                    }
                }
            }
        }

        private void Entities_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Entity entity in e.NewItems)
                    {
                        Add(entity);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Entity entity in e.OldItems)
                    {
                        Remove(entity);
                    }
                    break;
            }
        }

        private void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (EntityComponent entityComponent in e.NewItems)
                    {
                        Add(entityComponent);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (EntityComponent entityComponent in e.OldItems)
                    {
                        Remove(entityComponent);
                    }
                    break;
            }
        }
    }
}
