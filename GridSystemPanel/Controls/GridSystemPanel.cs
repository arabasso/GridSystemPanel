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
    [ProvideProperty("LayoutBreak", typeof(Control))]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class GridSystemPanel :
        Panel, IExtenderProvider
    {
        private GridSystemLayout _layoutEngine;
        public override LayoutEngine LayoutEngine => _layoutEngine ?? (_layoutEngine = new GridSystemLayout());

        private GridSystemPanelLayoutResolution _layoutResolution = GridSystemPanelLayoutResolution.Bootstrap;
        private bool _autoSize;

        [Category("Layout")]
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

        [Category("Layout")]
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

        [Category("Layout")]
        public GridSystemPanelLayout GetLayoutColumn(
            Control control)
        {
            return _layoutEngine.GetLayoutColumn(control);
        }

        [Category("Layout")]
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

        [Category("Layout")]
        public GridSystemPanelLayout GetLayoutColumnOffset(
            Control control)
        {
            return _layoutEngine.GetLayoutColumnOffset(control);
        }

        [Category("Layout")]
        public void SetLayoutBreak(
            Control control,
            GridSystemPanelLayoutBreak layoutBreak)
        {
            _layoutEngine.SetLayoutBreak(control, layoutBreak);

            if (IsHandleCreated)
            {
                PerformLayout();
            }
        }

        [Category("Layout")]
        public GridSystemPanelLayoutBreak GetLayoutBreak(
            Control control)
        {
            return _layoutEngine.GetLayoutBreak(control);
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

        public override Size GetPreferredSize(
            Size proposedSize)
        {
            if (!Controls.OfType<Control>().Any(w => w.Visible)) return Size;

            var parentDisplayRectangle = DisplayRectangle;

            var width = ClientSize.Width - Padding.Horizontal;

            var top = Padding.Top;
            var left = 0.0f;
            var maxHeight = 0;

            foreach (var control in Controls.OfType<Control>().Where(w => w.Visible).Reverse())
            {
                var controlPreferredSize = control.GetPreferredSize(parentDisplayRectangle.Size);

                var height = control.AutoSize
                    ? controlPreferredSize.Height
                    : control.Size.Height;

                var finalWidth = LayoutResolution.Compute(width, _layoutEngine.GetLayoutColumn(control)) - control.Margin.Horizontal;

                left += LayoutResolution.Compute(width, _layoutEngine.GetLayoutColumnOffset(control));

                if ((int)(left + control.Margin.Horizontal + finalWidth) > width || LayoutResolution.Compute(width, _layoutEngine.GetLayoutBreak(control)))
                {
                    left = 0.0f;
                    top += maxHeight;

                    maxHeight = 0;
                }

                maxHeight = Math.Max(height + control.Margin.Vertical, maxHeight);

                left += finalWidth + control.Margin.Horizontal;
            }

            return new Size(base.GetPreferredSize(proposedSize).Width, top + maxHeight + Padding.Bottom);
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
                var controlPreferredSize = control.GetPreferredSize(parentDisplayRectangle.Size);

                var height = control.AutoSize
                    ? controlPreferredSize.Height
                    : control.Size.Height;

                var finalWidth = parent.LayoutResolution.Compute(width, GetLayoutColumn(control)) - control.Margin.Horizontal;

                left += parent.LayoutResolution.Compute(width, GetLayoutColumnOffset(control));

                if ((int)(left + control.Margin.Horizontal + finalWidth) > width || parent.LayoutResolution.Compute(width, GetLayoutBreak(control)))
                {
                    left = 0.0f;
                    top += maxHeight;

                    maxHeight = 0;
                }

                maxHeight = Math.Max(height + control.Margin.Vertical, maxHeight);

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

                if (!parent.Controls.OfType<Control>().Any(w => w.Visible))
                {
                    height = parent.ClientSize.Height;
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
            if (!_layout.ContainsKey(control))
            {
                _layout.Add(control, new GridSystemPanelLayoutControl());
            }

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
            if (!_layout.ContainsKey(control))
            {
                _layout.Add(control, new GridSystemPanelLayoutControl());
            }

            return _layout[control].ColumnOffset;
        }

        public void SetLayoutBreak(
            Control control,
            GridSystemPanelLayoutBreak layoutBreak)
        {
            if (!_layout.ContainsKey(control))
            {
                _layout.Add(control, new GridSystemPanelLayoutControl
                {
                    Break = layoutBreak
                });
            }

            else
            {
                _layout[control].Break = layoutBreak;
            }
        }

        public GridSystemPanelLayoutBreak GetLayoutBreak(
            Control control)
        {
            if (!_layout.ContainsKey(control))
            {
                _layout.Add(control, new GridSystemPanelLayoutControl());
            }

            return _layout[control].Break;
        }

        private readonly Dictionary<Control, GridSystemPanelLayoutControl>
            _layout = new Dictionary<Control, GridSystemPanelLayoutControl>();
    }

    internal class GridSystemPanelLayoutControl
    {
        internal GridSystemPanelLayout Column;
        internal GridSystemPanelLayout ColumnOffset;
        internal GridSystemPanelLayoutBreak Break;

        public GridSystemPanelLayoutControl()
        {
            Column = new GridSystemPanelLayout(100, 50, 50, 50, 50);
            ColumnOffset = new GridSystemPanelLayout(0);
            Break = new GridSystemPanelLayoutBreak();
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

                    if (layout.ShouldSerializeAll())
                    {
                        return new InstanceDescriptor(typeof (GridSystemPanelLayout).GetConstructor(new[]
                        {
                            typeof (float)
                        }), new[]
                        {
                            (object) layout.All
                        });
                    }

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

            var padding = (GridSystemPanelLayout) context.PropertyDescriptor.GetValue(context.Instance);

            var propertyValue = (float) propertyValues["All"];

            if (Math.Abs(padding.All - propertyValue) > GridSystemPanelLayout.Tolerance)
            {
                return new GridSystemPanelLayout(propertyValue);
            }

            return new GridSystemPanelLayout((float)propertyValues["ExtraSmall"], (float)propertyValues["Small"], (float)propertyValues["Medium"], (float)propertyValues["Large"], (float)propertyValues["ExtraLarge"]);
        }

        public override PropertyDescriptorCollection GetProperties(
            ITypeDescriptorContext context,
            object value,
            Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(typeof(GridSystemPanelLayout), attributes).Sort(new[]
            {
                "All",
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
        internal const double Tolerance = 0.0001;

        public static readonly GridSystemPanelLayout Empty;

        private bool _all;
        private float _extraSmall;
        private float _small;
        private float _medium;
        private float _large;
        private float _extraLarge;

        [RefreshProperties(RefreshProperties.All)]
        public float All
        {
            get
            {
                if (!_all)
                    return -1;
                return _extraSmall;
            }
            set
            {
                var v = Math.Min(Math.Max(value, 0), 100);

                if (_all && Math.Abs(_extraSmall - v) < Tolerance)
                    return;

                _all = true;

                _extraSmall = _small = _medium = _large = _extraLarge = v;
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        public float ExtraSmall
        {
            get => _extraSmall;
            set
            {
                var v = Math.Min(Math.Max(value, 0), 100);

                if (!_all && Math.Abs(_extraSmall - v) < Tolerance)
                    return;

                _all = false;
                _extraSmall = v;
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        public float Small
        {
            get => _small;
            set
            {
                var v = Math.Min(Math.Max(value, 0), 100);

                if (!_all && Math.Abs(_small - v) < Tolerance)
                    return;

                _all = false;
                _small = v;
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        public float Medium
        {
            get => _medium;
            set
            {
                var v = Math.Min(Math.Max(value, 0), 100);

                if (!_all && Math.Abs(_medium - v) < Tolerance)
                    return;

                _all = false;
                _medium = v;
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        public float Large
        {
            get => _large;
            set
            {
                var v = Math.Min(Math.Max(value, 0), 100);

                if (!_all && Math.Abs(_large - v) < Tolerance)
                    return;

                _all = false;
                _large = v;
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        public float ExtraLarge
        {
            get => _extraLarge;
            set
            {
                var v = Math.Min(Math.Max(value, 0), 100);

                if (!_all && Math.Abs(_extraLarge - v) < Tolerance)
                    return;

                _all = false;
                _extraLarge = v;
            }
        }

        public GridSystemPanelLayout(
            float all)
        {
            _all = true;

            _extraSmall = _small = _medium = _large = _extraLarge = Math.Min(Math.Max(all, 0), 100);
        }

        public GridSystemPanelLayout(
            float extraSmall,
            float small,
            float medium,
            float large,
            float extraLarge)
        {
            _extraSmall = Math.Min(Math.Max(extraSmall, 0), 100);
            _small = Math.Min(Math.Max(small, 0), 100);
            _medium = Math.Min(Math.Max(medium, 0), 100);
            _large = Math.Min(Math.Max(large, 0), 100);
            _extraLarge = Math.Min(Math.Max(extraLarge, 0), 100);
            _all = Math.Abs(_extraSmall - _small) < Tolerance
                && Math.Abs(_extraSmall - _medium) < Tolerance
                && Math.Abs(_extraSmall - _large) < Tolerance
                && Math.Abs(_extraSmall - _extraLarge) < Tolerance;
        }

        internal bool ShouldSerializeAll()
        {
            return _all;
        }
    }

    public class GridSystemPanelLayoutBreakConverter :
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

            var numArray = new bool[strArray.Length];

            var converter = TypeDescriptor.GetConverter(typeof(bool));

            for (var index = 0; index < numArray.Length; ++index)
            {
                numArray[index] = (bool?)converter.ConvertFromString(context, culture, strArray[index]) ?? false;
            }

            if (numArray.Length == 5)
            {
                return new GridSystemPanelLayoutBreak(numArray[0], numArray[1], numArray[2], numArray[3], numArray[4]);
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

            if (value is GridSystemPanelLayoutBreak)
            {
                if (destinationType == typeof(string))
                {
                    var layoutBreak = (GridSystemPanelLayoutBreak)value;

                    if (culture == null) culture = CultureInfo.CurrentCulture;

                    var separator = culture.TextInfo.ListSeparator + " ";

                    var converter = TypeDescriptor.GetConverter(typeof(bool));

                    var strArray = new[]
                    {
                        converter.ConvertToString(context, culture, layoutBreak.ExtraSmall),
                        converter.ConvertToString(context, culture, layoutBreak.Small),
                        converter.ConvertToString(context, culture, layoutBreak.Medium),
                        converter.ConvertToString(context, culture, layoutBreak.Large),
                        converter.ConvertToString(context, culture, layoutBreak.ExtraLarge),
                    };

                    return string.Join(separator, strArray);
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    var layout = (GridSystemPanelLayoutBreak)value;

                    return new InstanceDescriptor(typeof(GridSystemPanelLayoutBreak).GetConstructor(new[]
                    {
                        typeof (bool),
                        typeof (bool),
                        typeof (bool),
                        typeof (bool),
                        typeof (bool),
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

            return new GridSystemPanelLayoutBreak((bool)propertyValues["ExtraSmall"], (bool)propertyValues["Small"], (bool)propertyValues["Medium"], (bool)propertyValues["Large"], (bool)propertyValues["ExtraLarge"]);
        }

        public override PropertyDescriptorCollection GetProperties(
            ITypeDescriptorContext context,
            object value,
            Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(typeof(GridSystemPanelLayoutBreak), attributes).Sort(new[]
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

    [TypeConverter(typeof(GridSystemPanelLayoutBreakConverter))]
    [ComVisible(true)]
    [Serializable]
    public struct GridSystemPanelLayoutBreak
    {
        public bool ExtraSmall { get; set; }

        public bool Small { get; set; }

        public bool Medium { get; set; }

        public bool Large { get; set; }

        public bool ExtraLarge { get; set; }

        public GridSystemPanelLayoutBreak(
            bool extraSmall,
            bool small,
            bool medium,
            bool large,
            bool extraLarge)
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

        public bool Compute(
            float width,
            GridSystemPanelLayoutBreak layoutBreak)
        {
            if (width < ExtraSmall)
            {
                return layoutBreak.ExtraSmall;
            }

            if (width < Small)
            {
                return layoutBreak.Small;
            }

            if (width < Medium)
            {
                return layoutBreak.Medium;
            }

            if (width < Large)
            {
                return layoutBreak.Large;
            }

            return layoutBreak.ExtraLarge;
        }
    }
}
