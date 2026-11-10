# GazeMagnet

**Cursor magnético pelo olhar no Windows 11, usando uma webcam comum e o GazePointer.**

O GazeMagnet é um projeto experimental de acessibilidade desenvolvido com ajuda de IA. Ele recebe as coordenadas do olhar pela GazeFlowAPI do GazePointer, identifica controles próximos usando o Windows UI Automation e aproxima o cursor de botões e links. Você pode clicar com o mouse, apertar **Enter** ou configurar um clique após manter o olhar no alvo.

## Requisitos

- Windows 11 de 64 bits (x64) e uma webcam.
- [GazePointer](https://sourceforge.net/projects/gazepointer/) instalado e calibrado no monitor principal. Ele é o responsável pelo rastreamento ocular.
- [.NET 10 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/10.0) para executar o GazeMagnet. Na página da Microsoft, escolha **Desktop Runtime** para Windows x64.

## Instalar e conectar

1. Baixe o **GazeMagnet.exe** na [página de releases](https://github.com/erickbrito-01/GazeMagnet/releases/latest).
2. Abra o GazePointer e calibre o olhar no monitor principal. Mantenha o rastreamento ligado, mas desligue **Control Mouse Cursor** (ícone de ponteiro). Desative também o marcador do olhar (ícone de olho) para evitar que ele cubra os controles.
3. Abra o GazeMagnet. Em **1. Conexão**, deixe **Configurar conexão automaticamente** marcada e clique em **Conectar**.
4. Se o GazePointer mostrar um pedido de acesso, confira o pedido e clique em **Allow** para autorizar. Aguarde o GazeMagnet indicar que está recebendo coordenadas.
5. Em **4. Controle do cursor**, confirme que fez a calibração e desligou o controle de cursor no GazePointer. Clique em **Assumir cursor**. Para devolver o controle ao mouse a qualquer momento, use **Ctrl + Alt + F12** ou **Parada de emergência**.

## Usar o magnetismo e o clique

- **Amarelo tracejado** indica um candidato. **Verde contínuo** indica que o cursor já grudou no alvo.
- Em **2. Magnetismo**, **Mostrar área de busca e mira** exibe o ponto do olhar e o raio usado para buscar controles. **Estabilizar olhar** reduz a oscilação. **Mover só entre alvos** mantém o cursor parado quando não há um novo alvo confirmado.
- Para experimentar o magnetismo sem câmera, marque **Demonstração com mouse (apenas destaque)** e aproxime o ponteiro de um botão. Desmarque essa opção para usar o olhar.
- Para clicar com o teclado, ative **Clicar no alvo verde com Enter**. O Enter mantém a função normal quando não há alvo fixado.
- Para experimentar o clique pelo olhar, selecione **Treino (sem clique)** em **3. Clique pelo olhar**. O preenchimento verde avança enquanto o alvo fixado estiver dentro do círculo. Em **Clique esquerdo**, o preenchimento completo dispara o clique. Afaste a mira até o alvo sair do círculo antes de repetir. **Ctrl + Alt + F11** pausa ou retoma o clique pelo olhar.

| Ajuste | Padrão | O que muda |
| --- | --- | --- |
| Raio | 56 px | Distância da borda do controle para atrair o cursor e contar o clique. |
| Fixação | 320 ms | Quanto tempo um candidato precisa permanecer selecionado antes de ficar verde. |
| Histerese | 24 px | Margem extra para manter o alvo fixado quando o olhar oscila. |
| Estabilizar olhar | Médio | Intensidade da suavização da mira e do cursor livre. |
| Tempo para clicar | 1,0 s | Quanto tempo o alvo precisa permanecer dentro do círculo depois de ficar verde. |

## Porta local, chave e reconexão

A configuração automática procura a porta do GazePointer no mesmo computador e usa a chave pública de teste `AppKeyDemo`. Para informar uma porta ou chave própria, desmarque **Configurar conexão automaticamente**. O endereço é `127.0.0.1`, e a porta padrão é **43333**. Se o GazePointer estiver em outra porta, confira o valor de `<port>` no arquivo `GazeStreamSettings.xml` da instalação. Use a mesma porta no GazeMagnet. [Documentação da GazeFlowAPI](https://github.com/szydej/GazeFlowAPI)

Na versão do GazePointer verificada durante o desenvolvimento, a chave demo limita a sessão a cerca de **10 minutos** e entrega cerca de **5 amostras por segundo**. Se a sessão cair ou expirar, o GazeMagnet tenta abrir outra até três vezes. O GazePointer pode pedir autorização novamente. O cursor só retoma o controle pelo olhar se já estava assumido e chegarem novas coordenadas estáveis. **Cancelar retomada** mantém o cursor manual. **Cancelar reconexão** interrompe as tentativas.

## Se algo falhar

| Sintoma | O que conferir |
| --- | --- |
| API indisponível | Confirme que o GazePointer está aberto e confira a porta local. |
| Acesso recusado | Revise a janela de autorização do GazePointer e a chave informada. |
| API conectada, sem coordenadas válidas | Confira a câmera, a posição do rosto e a calibração no monitor principal. |
| Um controle fica sem destaque | A detecção depende dos controles que cada aplicativo expõe ao Windows UI Automation. Interfaces personalizadas e janelas com privilégios elevados podem ter cobertura limitada. |

O GazeMagnet recebe as coordenadas pela conexão local e não as grava. A câmera é usada pelo GazePointer. Veja o [guia detalhado de operação e validação](LEIA-ME.md) para mais exemplos e limitações do MVP.

## Compilar a partir do código

Instale o [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) para Windows. Na raiz do repositório, execute no PowerShell:

```powershell
dotnet build .\src\GazeMagnet.csproj -c Release
dotnet run --project .\src\GazeMagnet.csproj
```

## Créditos

O GazeMagnet foi desenvolvido por **Erick Brito** com ajuda de IA. O rastreamento ocular vem do [GazePointer](https://sourceforge.net/projects/gazepointer/), publicado no SourceForge por **szydej**. O GazePointer é instalado separadamente.
