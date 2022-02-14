using OpenTK.Windowing.Common;

namespace GameEngine.Core
{
    /// <summary>
    /// Class, which implement Unity-like component.
    /// Components are the union of data and behaviour.
    /// This approach not ideal for complex systems,
    /// but it's enough for prototyping purposes.
    /// </summary>
    public abstract class Component
    {
        /// <summary>
        /// <see cref="Core.GameObject"/>, which own this component.
        /// </summary>
        public GameObject? GameObject { get; internal set; }

        /// <summary>
        /// Method which called when component instantiated and/or <see cref="Engine"/> has started.
        /// </summary>
        public virtual void Start() {}

        /// <summary>
        /// Engine loop tick method. Engine logic should be placed there.
        /// </summary>
        /// <param name="args"><see cref="FrameEventArgs"/></param>
        public virtual void GameUpdate(FrameEventArgs args) {}

        /// <summary>
        /// Render loop tick method. Render logic should be placed there.
        /// </summary>
        /// <param name="args"><see cref="FrameEventArgs"/></param>
        public virtual void RenderUpdate(FrameEventArgs args) {}

        /// <summary>
        /// Method which called when component removed or when <see cref="Engine"/> has stopped.
        /// </summary>
        public virtual void Stop() {}
    }
}