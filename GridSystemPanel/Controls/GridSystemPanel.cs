using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.Layout;

namespace GridSystemPanel.Controls
{
    [ProvideProperty("LayoutColumn", typeof(Control))]
    [ProvideProperty("LayoutColumnOffset", typeof(Control))]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class GridSystemPanel :
        Panel, IExtenderProvider
    {
        private GridSystemLayout _layoutEngine;
        public override LayoutEngine LayoutEngine => _layoutEngine ?? (_layoutEngine = new GridSystemLayout());

        private GridSystemPanelLayoutResolution _layoutResolution = GridSystemPanelLayoutResolution.Bootstrap;
        private bool _autoSize;

        public GridSystemPanelLayoutResolution LayoutResolution
        {
            get => _layoutResolution;
            set
            {
                _layoutResolution = value;

                if (IsHandleCreated)
                {
                    PerformLayout();
                }
            }
        }

        public void SetLayoutColumn(
            Control control,
            GridSystemPanelLayout layout)
        {
            _layoutEngine.SetLayoutColumn(control, layout);

            if (IsHandleCreated)
            {
                PerformLayout();
            }
        }

        public GridSystemPanelLayout GetLayoutColumn(
            Control control)
        {
            return _layoutEngine.GetLayoutColumn(control);
        }

        public void SetLayoutColumnOffset(
            Control control,
            GridSystemPanelLayout layout)
        {
            _layoutEngine.SetLayoutColumnOffset(control, layout);

            if (IsHandleCreated)
            {
                PerformLayout();
            }
        }

        public GridSystemPanelLayout GetLayoutColumnOffset(
            Control control)
        {
            return _layoutEngine.GetLayoutColumnOffset(control);
        }

        bool IExtenderProvider.CanExtend(
            object obj)
        {
            if (obj is Control control)
            {
                return control.Parent == this;
            }

            return false;
        }

        public override bool AutoSize
        {
            get => _autoSize;
            set
            {
                _autoSize = value;

                if (IsHandleCreated)
                {
                    PerformLayout();
                }
            }
        }
    }

    public class GridSystemLayout :
        LayoutEngine
    {
        public override bool Layout(
            object container,
            LayoutEventArgs layoutEventArgs)
        {
            var parent = (GridSystemPanel)container;

            var parentDisplayRectangle = parent.DisplayRectangle;

            var width = parent.ClientSize.Width - parent.Padding.Horizontal;

            var top = parent.Padding.Top;
            var left = 0.0f;
            var maxHeight = 0;

            foreach (var control in parent.Controls.OfType<Control>().Where(w => w.Visible).Reverse())
            {
                if (!_layout.ContainsKey(control))
                {
                    _layout.Add(control, new GridSystemPanelLayoutControl());
                }

                var controlPreferredSize = control.GetPreferredSize(parentDisplayRectangle.Size);

                var height = control.AutoSize
                    ? controlPreferredSize.Height
                    : control.Size.Height;

                var finalWidth = parent.LayoutResolution.Compute(width, _layout[control].Column) - control.Margin.Horizontal;

                if ((int)(left + control.Margin.Horizontal + finalWidth) > width)
                {
                    left = 0.0f;
                    top += maxHeight;

                    maxHeight = 0;
                }

                maxHeight = Math.Max(height + control.Margin.Vertical, maxHeight);

                left += parent.LayoutResolution.Compute(width, _layout[control].ColumnOffset);

                control.Location = new Point((int)(left + control.Margin.Left + parent.Padding.Left), top + control.Margin.Top);

                control.Size = new Size((int)finalWidth, height);

                left += finalWidth + control.Margin.Horizontal;
            }

            if (parent.AutoSize)
            {
                var height = top + maxHeight + parent.Padding.Bottom;

                if (parent.Dock == DockStyle.Bottom)
                {
                    parent.Location = new Point(parent.Location.X,  parent.Location.Y - height);
                }

                parent.ClientSize = new Size(parent.ClientSize.Width, height);
            }

            return parent.AutoSize;
        }

        public void SetLayoutColumn(
            Control control,
            GridSystemPanelLayout layout)
        {
            if (!_layout.ContainsKey(control))
            {
                _layout.Add(control, new GridSystemPanelLayoutControl
                {
                    Column = layout
                });
            }

            else
            {
                _layout[control].Column = layout;
            }
        }

        public GridSystemPanelLayout GetLayoutColumn(
            Control control)
        {
            return _layout[control].Column;
        }

        public void SetLayoutColumnOffset(
            Control control,
            GridSystemPanelLayout layout)
        {
            if (!_layout.ContainsKey(control))
            {
                _layout.Add(control, new GridSystemPanelLayoutControl
                {
                    ColumnOffset = layout
                });
            }

            else
            {
                _layout[control].ColumnOffset = layout;
            }
        }

        public GridSystemPanelLayout GetLayoutColumnOffset(
            Control control)
        {
            return _layout[control].ColumnOffset;
        }

        private readonly Dictionary<Control, GridSystemPanelLayoutControl>
            _layout = new Dictionary<Control, GridSystemPanelLayoutControl>();
    }

    internal class GridSystemPanelLayoutControl
    {
        internal GridSystemPanelLayout Column;
        internal GridSystemPanelLayout ColumnOffset;

        public GridSystemPanelLayoutControl()
        {
            Column = new GridSystemPanelLayout(100, 50, 50, 50, 50);
            ColumnOffset = new GridSystemPanelLayout(0, 0, 0, 0, 0);
        }
    }


    public class GridSystemPanelLayoutConverter :
        TypeConverter
    {
        public override bool GetCreateInstanceSupported(
            ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool CanConvertFrom(
            ITypeDescriptorContext context,
            Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(
            ITypeDescriptorContext context,
            Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value)
        {
            if (!(value is string str1)) return base.ConvertFrom(context, culture, value);

            var str2 = str1.Trim();

            if (str2.Length == 0) return null;

            var strArray = str2.Split(culture.TextInfo.ListSeparator[0]);

            var numArray = new float[strArray.Length];

            var converter = TypeDescriptor.GetConverter(typeof(float));

            for (var index = 0; index < numArray.Length; ++index)
            {
                numArray[index] = (float?)converter.ConvertFromString(context, culture, strArray[index]) ?? 0.0f;
            }

            if (numArray.Length == 5)
            {
                return new GridSystemPanelLayout(numArray[0], numArray[1], numArray[2], numArray[3], numArray[4]);
            }

            throw new ArgumentException($"TextParseFailedFormat {nameof(value)} {str2} ExtraSmall, Small, Medium, Large, ExtraLarge");
        }

        public override object ConvertTo(
          ITypeDescriptorContext context,
          CultureInfo culture,
          object value,
          Type destinationType)
        {
            if (destinationType == null) throw new ArgumentNullException(nameof(destinationType));

            if (value is GridSystemPanelLayout)
            {
                if (destinationType == typeof(string))
                {
                    var layout = (GridSystemPanelLayout)value;

                    if (culture == null) culture = CultureInfo.CurrentCulture;

                    var separator = culture.TextInfo.ListSeparator + " ";

                    var converter = TypeDescriptor.GetConverter(typeof(float));

                    var strArray = new[]
                    {
                        converter.ConvertToString(context, culture, layout.ExtraSmall),
                        converter.ConvertToString(context, culture, layout.Small),
                        converter.ConvertToString(context, culture, layout.Medium),
                        converter.ConvertToString(context, culture, layout.Large),
                        converter.ConvertToString(context, culture, layout.ExtraLarge),
                    };

                    return string.Join(separator, strArray);
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    var layout = (GridSystemPanelLayout)value;

                    return new InstanceDescriptor(typeof(GridSystemPanelLayout).GetConstructor(new[]
                    {
                        typeof (float),
                        typeof (float),
                        typeof (float),
                        typeof (float),
                        typeof (float),
                    }), new object[]
                    {
                        layout.ExtraSmall,
                        layout.Small,
                        layout.Medium,
                        layout.Large,
                        layout.ExtraLarge,
                    });
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object CreateInstance(
            ITypeDescriptorContext context,
            IDictionary propertyValues)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (propertyValues == null) throw new ArgumentNullException(nameof(propertyValues));

            return new GridSystemPanelLayout((float)propertyValues["ExtraSmall"], (float)propertyValues["Small"], (float)propertyValues["Medium"], (float)propertyValues["Large"], (float)propertyValues["ExtraLarge"]);
        }

        public override PropertyDescriptorCollection GetProperties(
            ITypeDescriptorContext context,
            object value,
            Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(typeof(GridSystemPanelLayout), attributes).Sort(new[]
            {
                "ExtraSmall",
                "Small",
                "Medium",
                "Large",
                "ExtraLarge"
            });
        }

        public override bool GetPropertiesSupported(
            ITypeDescriptorContext context)
        {
            return true;
        }
    }

    [TypeConverter(typeof(GridSystemPanelLayoutConverter))]
    [ComVisible(true)]
    [Serializable]
    public struct GridSystemPanelLayout
    {
        public static readonly GridSystemPanelLayout Empty;

        public float ExtraSmall { get; set; }
        public float Small { get; set; }
        public float Medium { get; set; }
        public float Large { get; set; }
        public float ExtraLarge { get; set; }

        public GridSystemPanelLayout(
            float extraSmall,
            float small,
            float medium,
            float large,
            float extraLarge)
        {
            ExtraSmall = extraSmall;
            Small = small;
            Medium = medium;
            Large = large;
            ExtraLarge = extraLarge;
        }
    }

    public class GridSystemPanelLayoutResolutionConverter :
        TypeConverter
    {
        public override bool GetCreateInstanceSupported(
            ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool CanConvertFrom(
            ITypeDescriptorContext context,
            Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(
            ITypeDescriptorContext context,
            Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value)
        {
            if (!(value is string str1)) return base.ConvertFrom(context, culture, value);

            var str2 = str1.Trim();

            if (str2.Length == 0) return null;

            var strArray = str2.Split(culture.TextInfo.ListSeparator[0]);

            var numArray = new int[strArray.Length];

            var converter = TypeDescriptor.GetConverter(typeof(int));

            for (var index = 0; index < numArray.Length; ++index)
            {
                numArray[index] = (int?)converter.ConvertFromString(context, culture, strArray[index]) ?? 0;
            }

            if (numArray.Length == 5)
            {
                return new GridSystemPanelLayoutResolution(numArray[0], numArray[1], numArray[2], numArray[3]);
            }

            throw new ArgumentException($"TextParseFailedFormat {nameof(value)} {str2} ExtraSmall, Small, Medium, Large");
        }

        public override object ConvertTo(
          ITypeDescriptorContext context,
          CultureInfo culture,
          object value,
          Type destinationType)
        {
            if (destinationType == null) throw new ArgumentNullException(nameof(destinationType));

            if (value is GridSystemPanelLayoutResolution)
            {
                if (destinationType == typeof(string))
                {
                    var layout = (GridSystemPanelLayoutResolution)value;

                    if (culture == null) culture = CultureInfo.CurrentCulture;

                    var separator = culture.TextInfo.ListSeparator + " ";

                    var converter = TypeDescriptor.GetConverter(typeof(int));

                    var strArray = new[]
                    {
                        converter.ConvertToString(context, culture, layout.ExtraSmall),
                        converter.ConvertToString(context, culture, layout.Small),
                        converter.ConvertToString(context, culture, layout.Medium),
                        converter.ConvertToString(context, culture, layout.Large),
                    };

                    return string.Join(separator, strArray);
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    var layout = (GridSystemPanelLayoutResolution)value;

                    return new InstanceDescriptor(typeof(GridSystemPanelLayoutResolution).GetConstructor(new[]
                    {
                        typeof (int),
                        typeof (int),
                        typeof (int),
                        typeof (int),
                    }), new object[]
                    {
                        layout.ExtraSmall,
                        layout.Small,
                        layout.Medium,
                        layout.Large,
                    });
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object CreateInstance(
            ITypeDescriptorContext context,
            IDictionary propertyValues)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (propertyValues == null) throw new ArgumentNullException(nameof(propertyValues));

            return new GridSystemPanelLayoutResolution((int)propertyValues["ExtraSmall"], (int)propertyValues["Small"], (int)propertyValues["Medium"], (int)propertyValues["Large"]);
        }

        public override PropertyDescriptorCollection GetProperties(
            ITypeDescriptorContext context,
            object value,
            Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(typeof(GridSystemPanelLayoutResolution), attributes).Sort(new[]
            {
                "ExtraSmall",
                "Small",
                "Medium",
                "Large"
            });
        }

        public override bool GetPropertiesSupported(
            ITypeDescriptorContext context)
        {
            return true;
        }
    }

    [TypeConverter(typeof(GridSystemPanelLayoutResolutionConverter))]
    [ComVisible(true)]
    [Serializable]
    public struct GridSystemPanelLayoutResolution
    {
        public static GridSystemPanelLayoutResolution Bootstrap => new GridSystemPanelLayoutResolution(576, 768, 992, 1200);
        public static GridSystemPanelLayoutResolution Material => new GridSystemPanelLayoutResolution(600, 968, 1280, 1920);
        private int _extraSmall;
        private int _small;
        private int _medium;
        private int _large;

        public int ExtraSmall
        {
            get => _extraSmall;
            set
            {
                _extraSmall = value;

                Validate();
            }
        }

        public int Small
        {
            get => _small;
            set
            {
                _small = value;

                Validate();
            }
        }

        public int Medium
        {
            get => _medium;
            set
            {
                _medium = value;

                Validate();
            }
        }

        public int Large
        {
            get => _large;
            set
            {
                _large = value;

                Validate();
            }
        }

        private void Validate()
        {
            // Less

            if (_large > 3 && _large <= _medium)
            {
                _medium = _large - 1;
            }

            if (_medium > 2 && _medium <= _small)
            {
                _small = _medium - 1;
            }

            if (_small > 1 && _small <= _extraSmall)
            {
                _extraSmall = _small - 1;
            }

            // Greater

            if (_extraSmall >= _small)
            {
                _small = _extraSmall + 1;
            }

            if (_small >= _medium)
            {
                _medium = _small + 1;
            }

            if (_medium >= _large)
            {
                _large = _medium + 1;
            }
        }

        public GridSystemPanelLayoutResolution(
            int extraSmall,
            int small,
            int medium,
            int large)
        {
            _extraSmall = extraSmall;
            _small = small;
            _medium = medium;
            _large = large;

            Validate();
        }

        public float Compute(
            float width,
            GridSystemPanelLayout layout)
        {
            if (width < ExtraSmall)
            {
                return (width * layout.ExtraSmall / 100.0f);
            }

            if (width < Small)
            {
                return (width * layout.Small / 100.0f);
            }

            if (width < Medium)
            {
                return (width * layout.Medium / 100.0f);
            }

            if (width < Large)
            {
                return (width * layout.Large / 100.0f);
            }

            return (width * layout.ExtraLarge / 100.0f);
        }
    }
}
