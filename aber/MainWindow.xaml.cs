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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Dsp;
using System.Diagnostics;


namespace aber
{

    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {

        WaveIn waveIn;
        DispatcherTimer timer;
        Stopwatch cronometro;

        float frecuenciaFundamental = 0;
        public MainWindow()
        {

            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
      

            cronometro = new Stopwatch();

            LlenarComboDispositivos();

        }

        private void Timer_Tick(object sender, EventArgs e)
        {

            if (frecuenciaFundamental >= 500)
            {
                var upArbol = Canvas.GetTop(imgArbol);
                Canvas.SetTop(imgArbol, upArbol + (frecuenciaFundamental / 500.0f) * 0.5f);
            }
            else
            {
                Canvas.SetTop(imgArbol, 10);
            }
        }


        public void LlenarComboDispositivos()
        {
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {

                WaveInCapabilities capacidades = WaveIn.GetCapabilities(i);
                cmbDispositivos.Items.Add(capacidades.ProductName);

            }

            cmbDispositivos.SelectedIndex = 0;

        }

        private void btnIniciar_Click(object sender, RoutedEventArgs e)
        {

            timer.Start();

            waveIn = new WaveIn();

            waveIn.WaveFormat = new WaveFormat(44100, 16, 1); 

            waveIn.BufferMilliseconds = 250;

            waveIn.DataAvailable += WaveIn_DataAvailable;

            waveIn.StartRecording();

        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {

            byte[] buffer = e.Buffer;
            int bytesGrabados = e.BytesRecorded;
            float acumulador = 0.0f;

            double numeroMuestras = bytesGrabados / 2;
            int exponente = 1;
            int numeroMuestrasComplejas = 0;
            int bitsMaximos = 0;

            do
            {
                bitsMaximos = (int)Math.Pow(2, exponente);
                exponente++;

            } while (bitsMaximos < numeroMuestras);

            numeroMuestrasComplejas = bitsMaximos / 2;
            exponente -= 2;

            Complex[] señalCompleja = new Complex[numeroMuestrasComplejas];

            for (int i = 0; i < bytesGrabados; i += 2)
            {

             
                short muestra = (short)(buffer[i + 1] << 8 | buffer[i]);
                float muestra32bits = (float)muestra / 32768.0f;
                acumulador += Math.Abs(muestra32bits);

                if (i / 2 < numeroMuestrasComplejas)
                {
                    señalCompleja[i / 2].X = muestra32bits;
                }

            }

            float promedio = acumulador / (bytesGrabados / 2.0f);
            sldMicrofono.Value = (double)promedio;

       

            if (promedio > 0)
            {

                FastFourierTransform.FFT(true, exponente, señalCompleja);
                float[] valoresAbsolutos = new float[señalCompleja.Length];
                for (int i = 0; i < señalCompleja.Length; i++)
                {

                    valoresAbsolutos[i] = (float)Math.Sqrt(
                        (señalCompleja[i].X * señalCompleja[i].X) +
                        (señalCompleja[i].Y * señalCompleja[i].Y)
                        );

                }
                int indiceSeñalConMasPresencia = valoresAbsolutos.ToList().IndexOf(valoresAbsolutos.Max());

                frecuenciaFundamental = (float)(indiceSeñalConMasPresencia * waveIn.WaveFormat.SampleRate) / (float)valoresAbsolutos.Length;
                lblFrecuencia.Text = frecuenciaFundamental.ToString("f");
            }

        }


        private void btnDetener_Click(object sender, RoutedEventArgs e)
        {
            waveIn.StopRecording();
        }

    }

}
