using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Svg;
using System.Drawing;
using System.Timers;

namespace _StreamDeckPlugin_
{
    [PluginActionId("$(UUID).action")]
    public class $(PluginName)Action : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings() => new PluginSettings { ResumeOnClick = false };

            [JsonProperty(PropertyName = "resumeOnClick")]
            public bool ResumeOnClick { get; set; }
        }

        private const int RESET_COUNTER_KEYPRESS_LENGTH = 1;

        private System.Timers.Timer tmrStopwatch;
        private PluginSettings settings;
        private bool keyPressed = false;
        private DateTime keyPressStart;
        private long stopwatchSeconds;

        public $(PluginName)Action(SDConnection connection, InitialPayload payload)
		: base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                Connection.SetSettingsAsync(JObject.FromObject(settings));
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
            ResetCounter();
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) => Tools.AutoPopulateSettings(settings, payload.Settings);

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }


        public override async void KeyPressed(KeyPayload payload)
        {
            // Used for long press
            keyPressStart = DateTime.Now;
            keyPressed = true;

            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");

            if (tmrStopwatch != null && tmrStopwatch.Enabled)
            {
                PauseStopwatch();
            }
            else
            {
                if (!settings.ResumeOnClick)
                {
                    ResetCounter();
                }

                ResumeStopwatch();
            }

            await RenderButton();
        }

        public override void KeyReleased(KeyPayload payload)
        {
            keyPressed = false;
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Released");
        }

        public async override void OnTick()
        {
            await RenderButton();            
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        private async Task RenderButton()
        {
            long total, minutes, seconds, hours;
            
            CheckIfResetNeeded();

            total = stopwatchSeconds;
            minutes = total / 60;
            seconds = total % 60;
            hours = minutes / 60;
            minutes = minutes % 60;

            var doc = new SvgDocument
            {
                Width = 72,
                Height = 72,
                ViewBox = new SvgViewBox(0, 0, 72, 72),
            };
            
            doc.Children.Add(new SvgRectangle()
            {
                Fill = new SvgColourServer(tmrStopwatch != null && tmrStopwatch.Enabled ? Color.Red : Color.Yellow),
                X = 0,
                Y = 0,
                Height = 72,
                Width = 72,
            });

            doc.Children.Add(new SvgText("Hallo")
            {
                FontSize = 25,
                TextAnchor = SvgTextAnchor.Middle,
                FontWeight = SvgFontWeight.Bold,
                Color = new SvgColourServer(Color.Blue),
                X = new SvgUnitCollection { new SvgUnit(SvgUnitType.Pixel, 36) },
                Y = new SvgUnitCollection { new SvgUnit(SvgUnitType.Pixel, 30) },
            });

            doc.Children.Add(new SvgText(hours.ToString("00"))
            {
                FontSize = 15,
                TextAnchor = SvgTextAnchor.Start,
                FontWeight = SvgFontWeight.Normal,
                Color = new SvgColourServer(Color.Blue),
                X = new SvgUnitCollection { new SvgUnit(SvgUnitType.Pixel, 7) },
                Y = new SvgUnitCollection { new SvgUnit(SvgUnitType.Pixel, 55) },
            });

            doc.Children.Add(new SvgText(minutes.ToString("00"))
            {
                FontSize = 15,
                TextAnchor = SvgTextAnchor.Middle,
                FontWeight = SvgFontWeight.Normal,
                Color = new SvgColourServer(Color.Blue),
                X = new SvgUnitCollection { new SvgUnit(SvgUnitType.Pixel, 36) },
                Y = new SvgUnitCollection { new SvgUnit(SvgUnitType.Pixel, 55) },
            });

            doc.Children.Add(new SvgText(seconds.ToString("00"))
            {
                FontSize = 15,
                TextAnchor = SvgTextAnchor.End,
                FontWeight = SvgFontWeight.Normal,
                Color = new SvgColourServer(Color.Blue),
                X = new SvgUnitCollection { new SvgUnit(SvgUnitType.Pixel, 65) },
                Y = new SvgUnitCollection { new SvgUnit(SvgUnitType.Pixel, 55) },
            });

            using MemoryStream ms = new MemoryStream();
            doc.Write(ms);
            ms.Position = 0;
            using var reader = new StreamReader(ms, System.Text.Encoding.UTF8);
            var content = reader.ReadToEnd();
            
            await Connection.SetImageAsync($"data:image/svg+xml;charset=utf8,{content}");
        }

        private void ResetCounter()
        {
            stopwatchSeconds = 0;
        }

        private void ResumeStopwatch()
        {
            if (tmrStopwatch is null)
            {
                tmrStopwatch = new System.Timers.Timer();
                tmrStopwatch.Elapsed += TmrStopwatch_Elapsed;
            }
            tmrStopwatch.Interval = 1000;
            tmrStopwatch.Start();
        }

        private void CheckIfResetNeeded()
        {
            if (!keyPressed)
            {
                return;
            }

            if ((DateTime.Now - keyPressStart).TotalSeconds > RESET_COUNTER_KEYPRESS_LENGTH)
            {
                PauseStopwatch();
                ResetCounter();
            }
        }

        private void TmrStopwatch_Elapsed(object? sender, ElapsedEventArgs e)
        {
            stopwatchSeconds++;
        }

        private void PauseStopwatch()
        {
            tmrStopwatch.Stop();
        }
    }
}
