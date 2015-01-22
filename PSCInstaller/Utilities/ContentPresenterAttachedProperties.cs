using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PSCInstaller.Utilities
{
    public static class ContentPresenterAttachedProperties 
    {
        /// <summary>
        /// ButtonTextForegroundProperty is a property used to adjust the color of text contained within the button.
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.RegisterAttached(
            "TextBlockForeground",
            typeof(Color),
            typeof(ContentPresenterAttachedProperties),
            new FrameworkPropertyMetadata(Color.FromArgb(255, 0, 0, 0), FrameworkPropertyMetadataOptions.AffectsRender, OnTextBlockForegroundChanged));

        public static void SetTextBlockForeground(FrameworkElement element, Color value)
        {
            if (element == null)
            {
                return;
            }

            if (element is TextBlock)
            {
                ((TextBlock)element).Foreground = new SolidColorBrush(value);
            }

            var children = VisualTreeHelper.GetChildrenCount(element as DependencyObject);
            if (children > 0)
            {
                for (int i = 0; i < children; i++)
                {
                    var child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                    if (child != null)
                    {
                        SetTextBlockForeground(child, value);
                    }
                }
            }
            else if (element is ContentPresenter)
            {
                SetTextBlockForeground(((ContentPresenter)element).Content as FrameworkElement, value);
            }

            element.SetValue(ForegroundProperty, value);
        }
        public static Color GetTextBlockForeground(UIElement element)
        {
            return (Color)element.GetValue(ForegroundProperty);
        }



        public static void OnTextBlockForegroundChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Color)
            {
                SetTextBlockForeground(o as FrameworkElement, (Color)e.NewValue);
            }
        }
    }
}
