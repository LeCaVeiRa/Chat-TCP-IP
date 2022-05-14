using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections;

namespace ChatServidor
{
    public class StatusChangedEventsArgs : EventArgs
    {
        private string EventMsg;

        public string EventMessage
        {
            get { return EventMsg; }
            set { EventMsg = value; }
        }

        public StatusChangedEventsArgs(string strEventMsg)
        {
            EventMsg = strEventMsg;
        }

    }

    public delegate void StatusChangedEventHandler(object sender, StatusChangedEventsArgs e);


    class ChatServidor
    {
        public static Hashtable htUsuarios = new Hashtable(30);
        public static Hashtable htConexoes = new Hashtable(30);

        private IPAddress enderecoIP;
        private int Nporta;
        private TcpClient tcpCliente;

        public static event StatusChangedEventHandler StatusChanged;
        private static StatusChangedEventsArgs e;

        public ChatServidor(IPAddress endereco, int porta)
        {
            enderecoIP = endereco;
            Nporta = porta;
        }

        public void porta(int port)
        {

        }

        private Thread thrListener;
        private TcpListener tlsCliente;

        bool ServRodando = false;

        public static void IncluiUsuario(TcpClient tcpUsuario, string strUsername)
        {
            ChatServidor.htUsuarios.Add(strUsername, tcpUsuario);
            ChatServidor.htConexoes.Add(tcpUsuario, strUsername);

            EnviaMensagemAdmin(htConexoes[tcpUsuario] + " Entrou...");
        }

        public static void RemoveUsuario(TcpClient tcpUsuario)
        {
            if(htConexoes[tcpUsuario] != null)
            {
                EnviaMensagemAdmin(htConexoes[tcpUsuario] + " Saiu...");

                ChatServidor.htUsuarios.Remove(ChatServidor.htConexoes[tcpUsuario]);
                ChatServidor.htConexoes.Remove(tcpUsuario);
            }
        }

        public static void OnStatusChanged(StatusChangedEventsArgs e)
        {
            StatusChangedEventHandler statusHandler = StatusChanged;
            if( statusHandler != null)
            {
                statusHandler(null, e);
            }
        }

        public static void EnviaMensagemAdmin(string Mensagem)
        {
            StreamWriter swSenderSender;

            e = new StatusChangedEventsArgs("Administrador: " + Mensagem);
            OnStatusChanged(e);

            TcpClient[] tcpClientes = new TcpClient[ChatServidor.htUsuarios.Count];
            ChatServidor.htUsuarios.Values.CopyTo(tcpClientes, 0);

            for (int i = 0; i < tcpClientes.Length; i++)
            {
                try
                {
                    if (Mensagem.Trim() == "" || tcpClientes[i] == null)
                    {
                        continue;
                    }

                    swSenderSender = new StreamWriter(tcpClientes[i].GetStream());
                    swSenderSender.WriteLine("Administrador: " + Mensagem);
                    swSenderSender.Flush();
                    swSenderSender = null;
                }
                catch
                {
                    RemoveUsuario(tcpClientes[i]);
                }
            }
        }

        public static void EnviaMensagem(string Origem, string Mensagem)
        {
            StreamWriter swSenderSender;

            e = new StatusChangedEventsArgs(Origem + " Disse: " + Mensagem);
            OnStatusChanged(e);

            TcpClient[] tcpClientes = new TcpClient[ChatServidor.htUsuarios.Count];

            ChatServidor.htUsuarios.Values.CopyTo(tcpClientes, 0);

            for (int i = 0; i < tcpClientes.Length; i++)
            {
                try
                {
                    if (Mensagem.Trim() =="" || tcpClientes[i] == null)
                    {
                        continue;
                    }

                    swSenderSender = new StreamWriter(tcpClientes[i].GetStream());
                    swSenderSender.WriteLine(Origem + " Disse: " + Mensagem);
                    swSenderSender.Flush();
                    swSenderSender = null;
                }
                catch
                {
                    RemoveUsuario(tcpClientes[i]);
                }
            }
        }

        public void IniciaAtendimento()
        {
            try
            {
                IPAddress ipaLocal = enderecoIP;
                int portalocal = Nporta;

                tlsCliente = new TcpListener(ipaLocal, Nporta);

                tlsCliente.Start();

                ServRodando = true;

                thrListener = new Thread(MantemAtendimento);
                thrListener.Start();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void MantemAtendimento()
        {
            while(ServRodando == true)
            {
                tcpCliente = tlsCliente.AcceptTcpClient();

                Conexao newConnection = new Conexao(tcpCliente);
            }
        }
    }

    class Conexao
    {
        TcpClient tcpCliente;

        private Thread thrSender;
        private StreamReader srReceptor;
        private StreamWriter swEnviador;
        private string usuarioAtual;
        private string strResposta;

        public Conexao(TcpClient tcpCon)
        {
            tcpCliente = tcpCon;
            thrSender = new Thread(AceitaCliente);

            thrSender.Start();
        }

        private void FechaConexao()
        {
            tcpCliente.Close();
            srReceptor.Close();
            swEnviador.Close();

        }

        private void AceitaCliente()
        {
            srReceptor = new System.IO.StreamReader(tcpCliente.GetStream());
            swEnviador = new System.IO.StreamWriter(tcpCliente.GetStream());

            usuarioAtual = srReceptor.ReadLine();

            if(usuarioAtual != "")
            {
                if(ChatServidor.htUsuarios.Contains(usuarioAtual) == true)
                {
                    swEnviador.WriteLine("0|Este nome de usuário já existe.");
                    swEnviador.Flush();
                    FechaConexao();
                    return;
                }
                else if (usuarioAtual == "Administrador")
                {
                    swEnviador.WriteLine("0|Este nome de usuário é reservado.");
                    swEnviador.Flush();
                    FechaConexao();
                    return;
                }
                else
                {
                    swEnviador.WriteLine("1");
                    swEnviador.Flush();

                    ChatServidor.IncluiUsuario(tcpCliente, usuarioAtual);
                }
            }
            else
            {
                FechaConexao();
                return;
            }

            try
            {
                while((strResposta = srReceptor.ReadLine()) != "")
                {
                    if (strResposta == null)
                    {
                        ChatServidor.RemoveUsuario(tcpCliente);
                    }
                    else
                    {
                        ChatServidor.EnviaMensagem(usuarioAtual, strResposta);
                    }
                }
            }
            catch
            {
                ChatServidor.RemoveUsuario(tcpCliente);
            }


        }

    }

}
