using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatServidor
{
    public partial class Form1 : Form
    {
        private delegate void AtualizaStatusCallBack(string strMensagem);
        public delegate void FileRecieveEventHandler(object fonte, string NomeArquivo);
        public event FileRecieveEventHandler NovoArquivoRecebido;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.NovoArquivoRecebido += new FileRecieveEventHandler(Form1_NovoArquivoRecebido);
        }

        private void Form1_NovoArquivoRecebido(object fonte, string nomeArquivo)
        {
            this.BeginInvoke(
                new Action(
                    delegate ()
                    {
                        MessageBox.Show("Novo Arquiv Recebido\n" + nomeArquivo);
                        System.Diagnostics.Process.Start("explorer", @"C:\");
                    }));
        }

        private void btnStart_Click(object sender, EventArgs e)
        {

            int porta = int.Parse(txtPort.Text);
            string endIP = txtIP.Text;

            try
            {
                IPAddress enderecoIP = IPAddress.Parse(txtIP.Text);
                int numeroporta = int.Parse(txtPort.Text);
                Task.Factory.StartNew(() => TratamentoArquivoRecebido(porta, endIP));

                ChatServidor mainServidor = new ChatServidor(enderecoIP, numeroporta);

                ChatServidor.StatusChanged += new StatusChangedEventHandler(mainServidor_StatusChanged);

                mainServidor.IniciaAtendimento();

                txtLog.AppendText("Monitoreando as Conexões...\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro de Conexão: " + ex.Message);
            }

        }

        public void TratamentoArquivoRecebido(int porta, string enderecoIP)
        {
            try
            {
                IPAddress ip = IPAddress.Parse(enderecoIP);
                TcpListener tcpListener = new TcpListener(ip, porta);
                tcpListener.Start();

                
                    Socket manipularSocket = tcpListener.AcceptSocket();
                    if (manipularSocket.Connected)
                    {
                        string nomeArquivo = string.Empty;
                        NetworkStream networkStream = new NetworkStream(manipularSocket);
                        int thisRead = 0;
                        int blockSize = 1024;
                        Byte[] dataByte = new byte[blockSize];
                        lock (this)
                        {
                            string caminhoPastaDestino = @"c:\dados";
                            manipularSocket.Receive(dataByte);
                            int tamanhoNomeArquivo = BitConverter.ToInt32(dataByte, 0);
                            nomeArquivo = Encoding.ASCII.GetString(dataByte, 4, tamanhoNomeArquivo);

                            Stream fileStream = File.OpenWrite(caminhoPastaDestino + nomeArquivo);
                            fileStream.Write(dataByte, 4 + tamanhoNomeArquivo, (1024 - (4 + tamanhoNomeArquivo)));

                            while (true)
                            {
                                thisRead = networkStream.Read(dataByte, 0, blockSize);
                                fileStream.Write(dataByte, 0, thisRead);
                                if(thisRead == 0)
                                {
                                    break;
                                }
                            }
                            fileStream.Close();
                        }
                        NovoArquivoRecebido?.Invoke(this, nomeArquivo);
                        manipularSocket = null;
                    }
                
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public void mainServidor_StatusChanged(object sender, StatusChangedEventsArgs e)
        {
            this.Invoke(new AtualizaStatusCallBack(this.AtualizaStatus), new object[] {e.EventMessage});
        }

        private void AtualizaStatus (string strMensagem)
        {
            txtLog.AppendText(strMensagem + "\r\n");
        }

        private void txtIP_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtPort_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtLog_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
