# 🦖 Dino Jump Interativo (Projeto Comunitário)

Projeto de extensão universitária desenvolvido em **C# (.NET 8 Windows Forms)** com **Visão Computacional (OpenCvSharp4)**. O jogo recria a clássica mecânica do dinossauro, permitindo que os participantes joguem usando o movimento do corpo (pulando em frente à câmera) ou pelo teclado.

---

## 📋 Sumário
1. [Sobre o Projeto](#-sobre-o-projeto)
2. [Pré-requisitos](#-pré-requisitos)
3. [Como Instalar e Executar (Passo a Passo)](#-como-instalar-e-executar-passo-a-passo)
4. [Tutorial de Uso e Como Jogar](#-tutorial-de-uso-e-como-jogar)
5. [Conexão da Câmera (Webcam ou Celular)](#-conexão-da-câmera-webcam-ou-celular)
6. [Privacidade e LGPD](#-privacidade-e-lgpd)
7. [Estrutura do Repositório](#-estrutura-do-repositório)

---

## 🎯 Sobre o Projeto

O **Dino Jump Comunitário** foi concebido para eventos de extensão, feiras de ciências e estandes comunitários. Seu objetivo é engajar o público de todas as idades de forma acessível e interativa, unindo computação gráfica, visão computacional e gamificação.


---

## 💻 Pré-requisitos

Para compilar e executar o código fonte na sua máquina:

1. **Sistema Operacional:** Windows 10 ou Windows 11 (64 bits).
2. **.NET 8.0 SDK:** [Baixar .NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
3. **Câmera:** Celular na mesma rede Wi-Fi.
4. *(Opcional)* **Visual Studio 2022** ou **Visual Studio Code** com extensão C# Dev Kit.

---

## 🚀 Como Instalar e Executar (Passo a Passo)

### Opção 1: Pelo Terminal / Linha de Comando (Recomendado)

1. **Clone o repositório:**
   ```bash
   git clone https://github.com/meijai/pratyc_4.git
   cd pratyc_4/DinoJumpComunitario
   ```

2. **Restaure as dependências do projeto:**
   ```bash
   dotnet restore
   ```

3. **Execute o programa:**
   ```bash
   dotnet run
   ```

---

### Opção 2: Pelo Visual Studio

1. Abra a pasta do projeto ou o arquivo `DinoJumpComunitario.csproj` no Visual Studio.
2. Aguarde a restauração dos pacotes NuGet (OpenCvSharp4).
3. Pressione `F5` ou clique no botão verde **Iniciar (DinoJumpComunitario)**.

---

## 🎮 Tutorial de Uso e Como Jogar

1. **Seleção de Câmera:**
   - Ao iniciar, pressione a tecla "D" que abrirá uma janela para escolha do dispositivo de captura de vídeo:
     - **Câmera Wi-Fi (Celular):** Insira a URL do stream (ex: `http://192.168.1.100:4747/video`).
   - Se preferir testar sem câmera, você pode fechar a janela de seleção e jogar diretamente pelo teclado.

2. **Iniciando a Partida:**
   - Na tela inicial, pressione **ESPAÇO**, **ENTER** ou dê um pulo na frente da câmera para iniciar a corrida.

3. **Controles durante o Jogo:**
   - **Pulo Físico:** Pule ou levante os braços na frente da câmera.
   - **Teclado:** Barra de **Espaço** ou **Seta para Cima** (`↑`).
   - **Pausa / Retomar:** Tecla **P** ou botão na interface.
   - **Reiniciar após Game Over:** Pressione **Espaço** ou pule novamente.

4. **Painel de Calibração (Webcam):**
   - Na lateral da tela, você pode visualizar o feed da câmera e o retângulo delimitador (ROI).
   - Ajuste o controle de **Sensibilidade** caso o ambiente tenha pouca luz ou excesso de movimento ao fundo.

---

## 📱 Conexão da Câmera (Webcam ou Celular)

1. Instale um aplicativo gratuito de câmera IP no celular, como **DroidCam** ou **IP Webcam** (disponíveis na Google Play Store e Apple App Store).
2. Conecte o celular e o computador na **mesma rede Wi-Fi**.
3. Abra o app no celular e anote o endereço exibido (exemplo: `http://192.168.0.15:4747/video`).
4. Digite esse endereço na tela de inicialização do Dino Jump.

---

## 🔒 Privacidade e LGPD

- **Sem gravação de vídeo/áudio:** O fluxo da câmera é processado frame a frame exclusivamente na memória RAM e descartado de imediato. Nenhuma imagem é salva no computador.
- **Sem armazenamento de dados pessoais:** Nenhum nome, documento ou identificação é solicitado aos usuários.
- **Sessão Volátil:** Ao fechar a aplicação, todos os dados da rodada e pontuações são limpos automaticamente.

---

## 📁 Estrutura do Repositório

```text
pratyc_4/
├── DinoJumpComunitario/            # Código-fonte da aplicação C#
│   ├── DinoJumpComunitario.csproj  # Configuração do projeto e dependências NuGet
│   ├── Program.cs                  # Ponto de entrada e tratamento global de erros
│   ├── Form1.cs                    # Lógica do jogo, física, renderização e OpenCV
│   ├── Form1.Designer.cs           # Componentes da interface Windows Forms
│   └── CameraSelectionForm.cs      # Diálogo para seleção de webcam/stream IP
├── Documentos/                     # Documentação acadêmica do projeto
│   ├── planejamento_projeto.docx   # Planejamento das etapas
│   ├── relatorio_pratic_iv.docx    # Relatório acadêmico da Prática IV
│   └── roteiro_do_projeto_...md    # Roteiro detalhado da ação comunitária
└── README.md                       # Tutorial de uso e instruções de instalação
```

---

## 👨‍💻 Autores
Desenvolvido como parte do projeto de extensão universitária **Prática IV**.
