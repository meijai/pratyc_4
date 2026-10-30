# Projeto Comunitário: Dino Jump Interativo (C# & Webcam)

**Público-Alvo:** Comunidade Geral / Qualquer Interessado (Estande Universitário, Feira de Extensão, Ação Social)  
**Ambiente de Desenvolvimento:** Visual Studio Code (.NET CLI)  
**Linguagem & Framework:** C# (.NET 8 Windows Forms)  
**Visão Computacional:** OpenCvSharp4  
**Persistência:** Nenhuma (100% em memória RAM — ao fechar o programa, tudo reseta)

---

## 1. Visão Geral da Arquitetura Enxuta

Sem dependências de bancos de dados externos (como MySQL), o projeto ganha:
* **Portabilidade total:** roda como um executável único em qualquer computador com Windows e webcam, sem precisar instalar ou iniciar serviços em segundo plano.
* **Privacidade total (LGPD compliance natural):** nenhum dado, imagem ou identificação pessoal de membros da comunidade é gravado em disco.
* **Zero configuração de rede/servidor:** basta clonar o repositório ou copiar a pasta e executar `dotnet run`.
* **Placar de Sessão Volátil:** o recorde da sessão (HighScore) é mantido em uma variável em memória (`static int SessionHighScore = 0`). Quando o aplicativo for fechado ao final do evento, os dados são descartados automaticamente.

---

## 2. Arquitetura da Solução

```
[ Webcam USB / Integrada ]
           │
           ▼
[ OpenCvSharp4 (Task Assíncrona) ] ── (Detecção de Movimento) ──┐
                                                                ▼
                                                [ Dino Game Engine (WinForms) ]
                                                                │
                                                                ▼
                                                [ Estado Volátil em RAM ]
                                                  - Pontuação Atual
                                                  - Recorde da Sessão
                                                  - Pulos da Rodada
```

---

## 3. Setup do Ambiente no Visual Studio Code

### 3.1 Pré-requisitos
1. **.NET 8 SDK** instalado ([dotnet.microsoft.com](https://dotnet.microsoft.com/download)).
2. **VS Code** com a extensão **C# Dev Kit** (ou extensão oficial da Microsoft para C#).
3. Webcam conectada e funcional.

### 3.2 Criação do Projeto via Terminal (.NET CLI)

Abra o terminal do seu computador (ou o terminal integrado do VS Code) e execute:

```bash
# 1. Criar projeto Windows Forms
dotnet new winforms -n DinoJumpComunitario
cd DinoJumpComunitario

# 2. Adicionar pacotes necessários de Visão Computacional (OpenCV)
dotnet add package OpenCvSharp4
dotnet add package OpenCvSharp4.Windows
dotnet add package OpenCvSharp4.Extensions
```

> **Nota:** Não é necessário instalar drivers de banco de dados ou pacotes ORM. Apenas os pacotes do OpenCV acima são necessários.

---

## 4. Estrutura de Arquivos no VS Code

Com a remoção do banco, a árvore de arquivos fica minimalista:

```
DinoJumpComunitario/
├── Form1.cs           # Tela do jogo, renderização gráfica e lógica do Dino
├── Form1.Designer.cs  # Inicialização de componentes visuais
├── Program.cs         # Ponto de entrada da aplicação
└── DinoJumpComunitario.csproj
```

*Dica prática:* Para facilitar o trabalho com IA no VS Code, você pode concentrar toda a lógica do jogo e da câmera diretamente dentro do `Form1.cs`.

---

## 5. Como Funciona a Detecção de Movimento (OpenCV)

Para que qualquer pessoa (criança, adulto ou idoso) consiga jogar com facilidade:

1. **Zona de Detecção (ROI - Region of Interest):** Define-se um retângulo visual na parte superior central do vídeo da webcam.
2. **Diferença de Quadros (`Cv2.Absdiff`):** O sistema compara o frame atual com o anterior. Se a pessoa pular (ou erguer os dois braços), pixels significativos entrarão na zona de detecção.
3. **Threshold & Contagem:** Quando a soma dos pixels em movimento ultrapassar uma sensibilidade configurável (ex: 15% da área), o jogo aciona o método `ExecutarPulo()`.
4. **Cooldown Antirruído:** Um temporizador simples (ex: 450 ms) impede disparos múltiplos enquanto a pessoa está descendo do pulo.

---

## 6. Roadmap Passo a Passo de Execução

```
[Etapa 1: Setup do Projeto no VS Code]
                 ⬇
[Etapa 2: Mecânica do Jogo (Física e Colisão)]
                 ⬇
[Etapa 3: Integração da Webcam com OpenCvSharp]
                 ⬇
[Etapa 4: Calibração de Sensibilidade & Feedback Visual]
                 ⬇
[Etapa 5: Validação Comunitária no Estande]
```

### Etapa 1: Setup no VS Code (Dia 1)
* Criar a pasta do projeto com os comandos do terminal.
* Rodar `dotnet run` para confirmar que a janela em branco padrão do Windows Forms abre normalmente.

### Etapa 2: Mecânica do Jogo (Dias 2 e 3)
* Criar o loop de jogo usando um `System.Windows.Forms.Timer` configurado para 16 ms (~60 FPS).
* Desenhar os elementos: Dinossauro (retângulo verde ou sprite), Chão (linha preta) e Cactos/Obstáculos (retângulos vermelhos móveis).
* Implementar a gravidade simples e o método `ExecutarPulo()`.
* Testar inicialmente o pulo com a tecla `Espaço`.
* Implementar o sistema de pontuação e o `HighScore` da sessão em memória.

### Etapa 3: Integração da Webcam (Dias 4 e 5)
* Abrir a câmera com `VideoCapture(0)` em uma `Task` separada para não congelar a interface gráfica.
* Mostrar uma pequena miniatura do feed da câmera no canto superior direito da tela do jogo com o retângulo de detecção desenhado em verde.
* Ligar o gatilho de movimento ao método `ExecutarPulo()`.

### Etapa 4: Calibração e Acessibilidade (Dia 6)
* Adicionar sliders ou atalhos de teclado (teclas `+` e `-`) para ajustar a sensibilidade da câmera no local do evento (locais com muita ou pouca luz exigem ajustes rápidos).
* Permitir acionamento por salto corporal OU erguimento de braços, garantindo acessibilidade para pessoas com limitações de mobilidade inferior ou cadeirantes.

### Etapa 5: Dia da Ação Comunitária
* Conectar o notebook a uma TV grande ou monitor externo via HDMI.
* Posicionar a câmera a cerca de 1,5 a 2 metros de distância do participante.
* Ao final do dia, basta fechar o jogo. Nenhum resíduo ou dado fica armazenado na máquina.

---

## 7. Prompts Prontos para Gerar o Código com IA

Copie e envie estes prompts na sequência para montar o seu jogo:

### Prompt 1: Jogo Base (Física, Renderização e Placar em Memória)

```text
Atue como um desenvolvedor C# especialista em .NET 8 e Windows Forms.
Preciso do código completo do arquivo Form1.cs para um clone simples do jogo do dinossauro do Chrome.

Requisitos:
1. Deve rodar no .NET 8 (Windows Forms).
2. Sem banco de dados: armazene 'Score' e 'HighScore' apenas em variáveis na memória RAM (resetar ao fechar).
3. Loop do jogo usando System.Windows.Forms.Timer a ~60 FPS.
4. Lógica de pulo do dinossauro com gravidade e obstáculos (cactos) se movendo da direita para a esquerda com velocidade incremental.
5. Detecção de colisão (Game Over). Pressionar 'Espaço' reinicia a rodada.
6. Crie um método público 'public void AcionarPulo()' para que um módulo externo de câmera possa ser plugado depois.
7. Renderização com DoubleBuffered = true para evitar oscilações na tela.
```

### Prompt 2: Módulo de Câmera com OpenCvSharp

```text
Atue como um desenvolvedor C# com experiência em OpenCvSharp4.
Complemente o Form1.cs anterior integrando a captura da webcam (VideoCapture):

Requisitos:
1. Inicializar a câmera de forma assíncrona para não travar a interface gráfica (WinForms).
2. Exibir o feed da webcam em um PictureBox pequeno no canto superior da tela.
3. Desenhar um retângulo (Região de Interesse - ROI) no terço superior do vídeo.
4. Detectar movimento dentro desse retângulo comparando frames consecutivos (Cv2.Absdiff).
5. Quando o movimento ultrapassar o limite, invocar o método 'AcionarPulo()'.
6. Adicionar um cooldown de 450 ms para evitar múltiplos pulos involuntários.
7. Liberar corretamente os recursos da câmera (Dispose/Release) ao fechar o formulário.
```

---

## 8. Dicas Práticas para o Dia do Estande Universitário

1. **Iluminação:** Evite colocar o jogador de costas para uma janela brilhante (luz de fundo cega a webcam). A iluminação deve vir de frente ou do teto.
2. **Marcação no Chão:** Cole uma fita crepe no chão indicando "Posicione-se Aqui" (distância ideal de ~2 metros da câmera).
3. **Modo Duplo:** Mantenha o teclado ativo (`Espaço`) como plano de contingência caso alguém prefira ou se houver instabilidade luminosa.