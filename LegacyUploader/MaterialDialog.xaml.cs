using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LegacyManagerUploader
{
    /// <summary>
    /// Interaction logic for DialogAuthentication.xaml
    /// </summary>
    public partial class MaterialDialog
    {
        public static class BUTTON_CAPTION
        {
            public const string OK = "OK";
            public const string NO = "NO";
            public const string CANCEL = "CANCEL";
        }
        public static class BUTTON_ID
        {
            public const int POSITIVE = 1;
            public const int NEGATIVE = 0;
            public const int NEUTRAL = -1;
        }

        private int ClickedButton = -1;

        // Routed events
        public static readonly RoutedEvent CloseEvent = EventManager.RegisterRoutedEvent(
        "DoClose", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MaterialDialog));
        private bool HasInputField = false;

        public event RoutedEventHandler DoClose
        {
            add { AddHandler(CloseEvent, value); }
            remove { RemoveHandler(CloseEvent, value); }
        }

        public MaterialDialog(string title, string content = "", int width = -1, int height = -1 )
        {
            InitializeComponent();

            tv_title.Content = title;
            tv_content.Text = content;

            // Set default visibility
            if(String.IsNullOrWhiteSpace(content))
            {
                tv_content.Visibility = Visibility.Collapsed;
            }
            tb_inputtext.Visibility = Visibility.Collapsed;

            btn_positive.Visibility = Visibility.Visible;
            btn_negative.Visibility = Visibility.Hidden;
            btn_neutral.Visibility = Visibility.Hidden;

            this.Focus();
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.PreviewKeyDown += new KeyEventHandler(HandleKeyboardInput);

            // Set SizeToContent mode depending on given dimensions
            if (width == -1 && height == -1)
            {
                this.SizeToContent = SizeToContent.WidthAndHeight;
            } else if(width == -1 && height != -1) {
                this.SizeToContent = SizeToContent.Width;
                this.Height = height;
            } else if(width != -1 && height == -1) {
                this.SizeToContent = SizeToContent.Height;
                this.Width = width;
            }
            //adjustViewBounds();
        }

        public MaterialDialogResult ShowReturnResult()
        {
            ShowDialog();

            MaterialDialogResult result = new MaterialDialogResult();
            result.ButtonId = ClickedButton;

            if (HasInputField)
            {
                result.TextInput = tb_inputtext.Text;
            }

            return result;
        }

        // Add buttons during runtime
        public void setButtonPositive(string caption = BUTTON_CAPTION.OK, Action action = null)
        {
            btn_positive.Click += (sender, e) => new Action(() => { ClickedButton = BUTTON_ID.POSITIVE; })(); // Basic functionality to determine which button was clicked

            // Add any custom button actions
            if (action != null)
            {
                btn_positive.Click += (sender, e) => action();
            }

            // Finish the button
            btn_positive.Content = caption;
            btn_positive.Visibility = Visibility.Visible;
        }

        public void setButtonNegative(string caption = BUTTON_CAPTION.NO, Action action = null)
        {
            btn_negative.Click += (sender, e) => new Action(() => { ClickedButton = BUTTON_ID.NEGATIVE; })(); // Basic functionality to determine which button was clicked

            // Add any custom button actions
            if (action != null)
            {
                btn_negative.Click += (sender, e) => action();
            }

            // Finish the button
            btn_negative.Content = caption;
            btn_negative.Visibility = Visibility.Visible;
        }

        public void setButtonNeutral(string caption = BUTTON_CAPTION.CANCEL, Action action = null)
        {
            btn_neutral.Click += (sender, e) => new Action(() => { ClickedButton = BUTTON_ID.NEUTRAL; })(); // Basic functionality to determine which button was clicked

            // Add any custom button actions
            if (action != null)
            {
                btn_neutral.Click += (sender, e) => action();
            }

            // Finish the button
            btn_neutral.Content = caption;
            btn_neutral.Visibility = Visibility.Visible;
        }

        // Set dialog's content during runtime
        public void setTitle(string _title)
        {
            tv_title.Content = _title;
        }

        public void setContent(string _content)
        {
            // TODO: Implement intelligent text wrapping
            tv_content.Visibility = Visibility.Visible;
            tv_content.Text = _content;
            //adjustViewBounds();
        }

        public void setInput(string _hint = "")
        {
            HasInputField = true;
            tb_inputtext.Visibility = Visibility.Visible;
            tb_inputtext.Text = _hint;
        }

        // UI Functionality
        private void HandleKeyboardInput(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Escape:
                    RaiseCloseEvent();
                    break;
                case Key.Enter:
                    ClickedButton = BUTTON_ID.POSITIVE;
                    RaiseCloseEvent();
                    break;
            }
        }
        private void btn_OnClick(object sender, RoutedEventArgs e)
        {
            RaiseCloseEvent();
        }
        private void SettingsWindow_DragMove(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        void RaiseCloseEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(MaterialDialog.CloseEvent);
            RaiseEvent(newEventArgs);
        }

        private void CloseAnimation_Completed(object sender, EventArgs e)
        {
            this.Close();
        }
    }

    public class MaterialDialogResult
    {
        public int ButtonId = 0;

        // Content
        public string TextInput = "";
        public int SelectedItem = -1;
    }
}
