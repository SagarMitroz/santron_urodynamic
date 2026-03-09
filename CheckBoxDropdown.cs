using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace SantronWinApp
{
    public class CheckBoxDropdown : UserControl
    {
        private ToolStripDropDown dropDown;
        private CheckedListBox checkedList;

        //public CheckBoxDropdown()
        //{
        //    SetStyle(ControlStyles.UserPaint |
        //             ControlStyles.AllPaintingInWmPaint |
        //             ControlStyles.OptimizedDoubleBuffer, true);

        //    Height = 28;
        //    TabStop = true;
        //    BackColor = Color.White;

        //    checkedList = new CheckedListBox
        //    {
        //        CheckOnClick = true,
        //        BorderStyle = BorderStyle.None
        //    };

        //    checkedList.ItemCheck += (s, e) =>
        //        BeginInvoke(new Action(UpdateText));

        //    dropDown = new ToolStripDropDown
        //    {
        //        Padding = Padding.Empty
        //    };

        //    dropDown.Items.Add(new ToolStripControlHost(checkedList)
        //    {
        //        AutoSize = false,
        //        Margin = Padding.Empty,
        //        Padding = Padding.Empty
        //    });
        //}

        public CheckBoxDropdown()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            Height = 28;
            TabStop = true;
            BackColor = Color.White;

            checkedList = new CheckedListBox
            {
                CheckOnClick = true,
                BorderStyle = BorderStyle.None
            };

            // ✅ IMPORTANT: hook correct handler
            checkedList.ItemCheck += CheckedList_ItemCheck;

            dropDown = new ToolStripDropDown
            {
                Padding = Padding.Empty
            };

            dropDown.Items.Add(new ToolStripControlHost(checkedList)
            {
                AutoSize = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            });
        }


        public bool SingleSelection { get; set; } = false;



        // 🔹 Enforce single-selection logic here
        //private void CheckedList_ItemCheck(object sender, ItemCheckEventArgs e)
        //{
        //    if (SingleSelection && e.NewValue == CheckState.Checked)
        //    {
        //        for (int i = 0; i < checkedList.Items.Count; i++)
        //        {
        //            if (i != e.Index)
        //                checkedList.SetItemChecked(i, false);
        //        }
        //    }

        //    BeginInvoke(new Action(UpdateText));
        //}
        private bool _internalChange = false;

        private void CheckedList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_internalChange)
                return;

            if (SingleSelection && e.NewValue == CheckState.Checked)
            {
                // Delay unchecking others until AFTER current check is applied
                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        _internalChange = true;

                        for (int i = 0; i < checkedList.Items.Count; i++)
                        {
                            if (i != e.Index)
                                checkedList.SetItemChecked(i, false);
                        }
                    }
                    finally
                    {
                        _internalChange = false;
                        UpdateText();
                    }
                }));
            }
            else
            {
                BeginInvoke(new Action(UpdateText));
            }
        }

        // 🔹 Draw like real ComboBox
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = ClientRectangle;

            ComboBoxState state =
                Focused ? ComboBoxState.Hot : ComboBoxState.Normal;

            ComboBoxRenderer.DrawTextBox(g, rect, state);

            Rectangle arrow = new Rectangle(
                rect.Right - 18, rect.Y + 4, 14, rect.Height - 8);

            ComboBoxRenderer.DrawDropDownButton(g, arrow, state);

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                new Rectangle(6, 0, rect.Width - 24, rect.Height),
                SystemColors.ControlText,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            ShowDropDown();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        private void ShowDropDown()
        {
            checkedList.Width = Width;
            checkedList.Height = Math.Min(160, checkedList.PreferredHeight);
            dropDown.Show(this, new Point(0, Height));
        }

        private void UpdateText()
        {
            Text = string.Join(", ",
                checkedList.CheckedItems.Cast<string>());
            Invalidate();
        }

        // 🔹 PUBLIC API (simple & clean)

        public void AddItems(params string[] items)
        {
            checkedList.Items.AddRange(items);
        }

        public bool IsChecked(int index)
        {
            return checkedList.GetItemChecked(index);
        }

        public void SetChecked(int index, bool value)
        {
            checkedList.SetItemChecked(index, value);
        }

        public void Clear()
        {
            checkedList.Items.Clear();
            Text = "";
        }

        public Font ItemFont
        {
            get => checkedList.Font;
            set => checkedList.Font = value;
        }


    }
}
