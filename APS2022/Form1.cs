using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace APS2022
{
    public partial class Form1 : Form
    {

        private string NomeUsuario = "Desconhecido";
        private StreamWriter stwEnviador;
        private StreamReader strReceptor;
        private TcpClient tcpServidor;

        private delegate void AtualizaLogCallback(string strMensagem);

        private delegate void FechaConexaoCallBack(string srtMotivo);
        private Thread mensagemThread;
        private IPAddress enderecoIP;
        private bool Conectado;

        private static string nomeAbreviadoArquivo = "";

        public Form1()
        {
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            InitializeComponent();
        }

        public void OnApplicationExit(object sender, EventArgs e)
        {
            if(Conectado == true)
            {
                Conectado = false;
                stwEnviador.Close();
                strReceptor.Close();
                tcpServidor.Close();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (Conectado == false)
                {
                    InicializaConexao();
                }
                else
                {
                    FechaConexao("Desconectado a pedido do usuário.");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }
        }

        private void InicializaConexao()
        {
            enderecoIP = IPAddress.Parse(txtIP.Text);
            int porta = int.Parse(txtPort.Text);
            tcpServidor = new TcpClient();
            tcpServidor.Connect(enderecoIP, porta);

            Conectado = true;

            NomeUsuario = txtNome.Text;

            txtIP.Enabled = false;
            txtNome.Enabled = false;
            txtPort.Enabled = false;
            txtChat.Enabled = true;
            txtMessage.Enabled = true;
            btnSend.Enabled = true;
            btnConnect.Text = "Desconectado.";

            stwEnviador = new StreamWriter(tcpServidor.GetStream());
            stwEnviador.WriteLine(txtNome.Text);
            stwEnviador.Flush();

            mensagemThread = new Thread(new ThreadStart(RecebeMensagens));
            mensagemThread.Start();
        }

        private void RecebeMensagens()
        {
            strReceptor = new StreamReader(tcpServidor.GetStream());
            string ConResposta = strReceptor.ReadLine();

            if(ConResposta[0] == '1')
            {
                this.Invoke(new AtualizaLogCallback(this.AtualizaLog), new object[] { "Conectado com Sucesso!" });
            }
            else
            {
                string Motivo = "Não Conectado: ";
                Motivo += ConResposta.Substring(2, ConResposta.Length - 2);

                this.Invoke(new FechaConexaoCallBack(this.FechaConexao), new object[] { Motivo });

                return;
            }

            while (Conectado)
            {
                this.Invoke(new AtualizaLogCallback(this.AtualizaLog), new object[] { strReceptor.ReadLine() });
            }
        }

        private void AtualizaLog(string strMensagem)
        {
            txtChat.AppendText(strMensagem + "\r\n");
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            EnviaMensagem();
        }

        private void EnviaMensagem()
        {
            if (txtMessage.Lines.Length >= 1)
            {
                stwEnviador.WriteLine(txtMessage.Text);
                stwEnviador.Flush();
                txtMessage.Lines = null;
            }
            txtMessage.Text = "";
        }

        private void FechaConexao(string Motivo)
        {
            txtChat.AppendText(Motivo + "\r\n");

            txtIP.Enabled = true;
            txtNome.Enabled = true;
            txtMessage.Enabled = false;
            btnSend.Enabled = false;
            btnConnect.Text = "Conectado.";

            Conectado = false;
            stwEnviador.Close();
            strReceptor.Close();
            tcpServidor.Close();

        }

        private void btnProcurar_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Envio de Arquivo - Cliente";
            dlg.ShowDialog();
            txtArquivo.Text = dlg.FileName;
            nomeAbreviadoArquivo = dlg.SafeFileName;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtIP.Text) && string.IsNullOrEmpty(txtPort.Text) && string.IsNullOrEmpty(txtArquivo.Text))
            {
                MessageBox.Show("Dados Inválidos...");
                return;
            }

            string enderecoIP = txtIP.Text;
            int porta = int.Parse(txtPort.Text);
            string nomeArquivo = txtArquivo.Text;
            
            try
            {
                Task.Factory.StartNew(() => EnviarArquivo(enderecoIP, porta, nomeArquivo, nomeAbreviadoArquivo));
                MessageBox.Show("Arquivo enviado com Sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }

        }

        public void EnviarArquivo(string IPHost, int PortHost, string nomeCaminhoArquivo, string nomeAbreviadoArquivo)
        {
            try
            {
                if (!string.IsNullOrEmpty(IPHost))
                {
                    byte[] fileNameByte = Encoding.ASCII.GetBytes(nomeAbreviadoArquivo);
                    byte[] fileData = File.ReadAllBytes(nomeCaminhoArquivo);
                    byte[] clientData = new byte[4 + fileNameByte.Length + fileData.Length];
                    byte[] fileNameLen = BitConverter.GetBytes(fileNameByte.Length);

                    fileNameLen.CopyTo(clientData, 0);
                    fileNameByte.CopyTo(clientData, 4);
                    fileData.CopyTo(clientData, 4 + fileNameByte.Length);

                    TcpClient clientSocket = new TcpClient(IPHost, PortHost);
                    NetworkStream networkStream = clientSocket.GetStream();

                    networkStream.Write(clientData, 0, clientData.GetLength(0));
                    networkStream.Close();
                }
            }
            catch
            {
                throw;
            }
        }

    }
}
