using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace GameEngine.Core
{
    /// <summary>
    /// Class implement engine object. Engine object is container for components.
    /// Components declare properties and behaviour of engine object.
    /// </summary>
    public class GameObject
    {
        private readonly List<GameObject> _children = new();
        private readonly Dictionary<Type, Component> _componentsMap = new();

        public readonly Engine Engine;

        public GameObject? Parent { get; private set; }

        public IReadOnlyList<GameObject> Children => _children;

        public Quaternion LocalRotation { get; set; } = Quaternion.Identity;

        public Vector3 LocalEuler
        {
            get => LocalRotation.ToEulerAngles() * 180 / MathHelper.Pi;
            set => LocalRotation = Quaternion.FromEulerAngles(value * MathHelper.Pi / 180);
        }

        public Vector3 LocalScale { get; set; } = new(1);

        public Vector3 LocalPosition { get; set; } = new(0);

        public Quaternion Rotation
        {
            get => Parent != null 
                ? LocalRotation * Parent.Rotation 
                : LocalRotation; 
            set => LocalRotation = Parent != null 
                ? Parent.Rotation.Inverted() * value
                : value;
        }

        public Vector3 Euler
        {
            get => Rotation.ToEulerAngles() * 180 / MathHelper.Pi;
            set => Rotation = Quaternion.FromEulerAngles(value * MathHelper.Pi / 180);
        }

        public Vector3 Scale
        {
            get => Parent != null 
                ? Parent.Scale * LocalScale 
                : LocalScale;
            set => LocalScale = Parent != null 
                ? new Vector3(value.X / Parent.Scale.X, value.Y / Parent.Scale.Y, value.Z / Parent.Scale.Z)
                : value;
        }

        public Vector3 Position
        {
            get
            {
                if (Parent == null) return LocalPosition;
                var offset = new Vector4(LocalPosition, 1);
                offset *= Parent.GetModelMatrix();
                return offset.Xyz;

            }
            set
            {
                if (Parent != null)
                {
                    var offset = new Vector4(value, 1);
                    offset *= Parent.GetModelMatrix().Inverted();
                    LocalPosition = offset.Xyz;
                }
                else
                {
                    LocalPosition = value;
                }
            }
        }

        public Matrix4 GetModelMatrix()
        {
            var model = Matrix4.Identity;
            model *= Matrix4.CreateScale(LocalScale);
            model *= Matrix4.CreateFromQuaternion(LocalRotation);
            model *= Matrix4.CreateTranslation(LocalPosition);

            if (Parent != null)
            {
                model *= Parent.GetModelMatrix();
            }

            return model;
        }

        public event Action<GameObject, Component>? ComponentAdded;
        public event Action<GameObject, Component>? ComponentRemoved;

        internal GameObject(Engine engine)
        {
            Engine = engine;
        }

        // TODO: replace with Parent property.
        public void AddChild(GameObject go)
        {
            if (go.Parent == this) return;
            go.Parent?.RemoveChild(go);
            go.Parent = this;
            _children.Add(go);
        }

        public void RemoveChild(GameObject go)
        {
            if (go.Parent == this)
            {
                go.Parent = null;
                _children.Remove(go);
            }
        }

        /// <summary>
        /// Method add new <see cref="Component"/> to <see cref="GameObject"/>.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Component"/>.</typeparam>
        /// <returns><see cref="Component"/>, added to <see cref="GameObject"/>.</returns>
        public T Add<T>() 
            where T : Component, new() 
            => (T) Add(typeof(T));

        /// <summary>
        /// Method add new <see cref="Component"/> to <see cref="GameObject"/>.
        /// </summary>
        /// <param name="componentType">Type of <see cref="Component"/>.</param>
        /// <returns><see cref="Component"/>, added to <see cref="GameObject"/>.</returns>
        /// <exception cref="ArgumentException"></exception>
        public Component Add(Type componentType)
        {
            if (Activator.CreateInstance(componentType) is not Component instance)
            {
                throw new ArgumentException(
                    $"Not valid type. It should inherit from {typeof(Component)} and have default constructor.");
            }

            if (_componentsMap.TryGetValue(componentType, out var component))
            {
                Remove(component.GetType());
            }
            
            _componentsMap[instance.GetType()] = instance;
            instance.GameObject = this;

            if (Engine.IsRun)
            {
                instance.Start();
            }

            ComponentAdded?.Invoke(this, instance);
            return instance;
        }

        /// <summary>
        /// Method return <see cref="Component"/> of specified type.
        /// </summary>
        /// <typeparam name="T"><see cref="Component"/> type.</typeparam>
        /// <returns><see cref="Component"/> if it's exist, or null.</returns>
        public T? Get<T>() where T : Component
            => (T?) Get(typeof(T));

        /// <summary>
        /// Method return <see cref="Component"/> of specified type.
        /// </summary>
        /// <param name="componentType"><see cref="Component"/> type.</param>
        /// <returns><see cref="Component"/> if it's exist, or null.</returns>
        public Component? Get(Type componentType)
            => _componentsMap.TryGetValue(componentType, out var component) ? component : null;

        /// <summary>
        /// Remove <see cref="Component"/> of specified type.
        /// </summary>
        /// <typeparam name="T"><see cref="Component"/> type.</typeparam>
        /// <returns><see cref="Component"/> if it was removed and null in other case.</returns>
        public T? Remove<T>() where T : Component
            => (T?) Remove(typeof(T));

        /// <summary>
        /// Remove <see cref="Component"/> of specified type.
        /// </summary>
        /// <param name="componentType"><see cref="Component"/> type.</param>
        /// <returns><see cref="Component"/> if it was removed and null in other case.</returns>
        public Component? Remove(Type componentType)
        {
            if (_componentsMap.TryGetValue(componentType, out var component))
            {
                _componentsMap.Remove(componentType);

                if (Engine.IsRun)
                {
                    component.Stop();
                }
                
                component.GameObject = null;
                ComponentRemoved?.Invoke(this, component);
                return component;
            }

            return null;
        }

        public void Start()
        {
            foreach (var component in _componentsMap.ToList())
            {
                if (component.Value.GameObject == this)
                {
                    component.Value.Start();
                }
            }
        }

        public void GameUpdate(FrameEventArgs args)
        {
            foreach (var component in _componentsMap.ToList())
            {
                if (component.Value.GameObject == this)
                {
                    component.Value.GameUpdate(args);
                }
            }
        }

        public void RenderUpdate(FrameEventArgs args)
        {
            foreach (var component in _componentsMap.ToList())
            {
                if (component.Value.GameObject == this)
                {
                    component.Value.RenderUpdate(args);
                }
            }
        }

        public void Stop()
        {
            foreach (var component in _componentsMap.ToList())
            {
                if (component.Value.GameObject == this)
                {
                    component.Value.Stop();
                }
            }
        }
    }
}
