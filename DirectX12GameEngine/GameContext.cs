﻿using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace DirectX12GameEngine
{
    public enum AppContextType
    {
        CoreWindow,
        Xaml
    }

    public abstract class GameContext
    {
        public AppContextType ContextType { get; protected set; }

        internal int RequestedHeight { get; private protected set; }

        internal int RequestedWidth { get; private protected set; }
    }

    public abstract class GameContext<TControl> : GameContext
    {
        public TControl Control { get; private protected set; }

        protected GameContext(TControl control, int requestedWidth = 0, int requestedHeight = 0)
        {
            Control = control;
            RequestedWidth = requestedWidth;
            RequestedHeight = requestedHeight;
        }
    }

    public class GameContextCoreWindow : GameContext<CoreWindow>
    {
        public GameContextCoreWindow(CoreWindow? control = null, int requestedWidth = 0, int requestedHeight = 0, bool isHolographic = false)
            : base(control ?? CoreWindow.GetForCurrentThread(), requestedWidth, requestedHeight)
        {
            ContextType = AppContextType.CoreWindow;

            if (requestedHeight == 0 || requestedWidth == 0)
            {
                RequestedWidth = (int)Control.Bounds.Width;
                RequestedHeight = (int)Control.Bounds.Height;
            }

            IsHolographic = isHolographic;
        }

        internal bool IsHolographic { get; }
    }

    public class GameContextXaml : GameContext<SwapChainPanel>
    {
        public GameContextXaml(SwapChainPanel control, int requestedWidth = 0, int requestedHeight = 0)
            : base(control, requestedWidth, requestedHeight)
        {
            ContextType = AppContextType.Xaml;

            if (requestedHeight == 0 || requestedWidth == 0)
            {
                RequestedWidth = (int)Control.ActualWidth;
                RequestedHeight = (int)Control.ActualHeight;
            }
        }
    }
}