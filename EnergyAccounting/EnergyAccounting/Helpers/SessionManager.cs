using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace EnergyAccounting.Helpers
{
    public static class SessionManager
    {
        private static DispatcherTimer _timer;
        private static DateTime _lastActivityTime;
        private static readonly TimeSpan _timeoutLimit = TimeSpan.FromMinutes(10); // 10 минут
        private static bool _isMonitoring = false;

        public static void StartMonitoring()
        {
            if (_isMonitoring) return;

            _lastActivityTime = DateTime.Now;
            _isMonitoring = true;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            InputManager.Current.PreProcessInput += OnGlobalInput;
        }

        public static void StopMonitoring()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
            InputManager.Current.PreProcessInput -= OnGlobalInput;
            _isMonitoring = false;
        }

        private static void OnGlobalInput(object sender, PreProcessInputEventArgs e)
        {
            if (e.StagingItem.Input is MouseEventArgs || e.StagingItem.Input is KeyboardEventArgs)
            {
                _lastActivityTime = DateTime.Now;
            }
        }

        private static void Timer_Tick(object sender, EventArgs e)
        {
            if ((DateTime.Now - _lastActivityTime) >= _timeoutLimit)
            {
                StopMonitoring(); // Останавливаем таймер

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Вы были неактивны более 10 минут. Система закрыта. Пожалуйста, авторизуйтесь снова.",
                                    "Безопасность", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // СКРЫВАЕМ все открытые окна (не закрываем!)
                    foreach (Window window in Application.Current.Windows.Cast<Window>().ToList())
                    {
                        window.Hide();
                    }

                    // Открываем окно авторизации и передаем флаг переавторизации
                    LoginWindow loginWindow = new LoginWindow(true);
                    loginWindow.Show();
                });
            }
        }
    }
}