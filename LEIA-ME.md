# GazeMagnet 0.7.15

MVP em C# para Windows 11. O GazePointer usa a câmera e fornece as coordenadas. O GazeMagnet encontra controles na janela visível sob o olhar, destaca o alvo e move o cursor quando você assume o controle. Você pode clicar manualmente ou habilitar o clique pelo olhar.

## Rodar no Windows 11

Abra **GazeMagnet.exe**. O executável é para Windows x64 e usa o **.NET 10 Desktop Runtime**. Ele já está disponível no computador usado para testar. Em outro computador, se o Windows pedir o runtime, baixe o **Desktop Runtime x64** na [página oficial do .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).

O painel escuro organiza a janela em **Conexão**, **Magnetismo**, **Clique pelo olhar** e **Controle do cursor**. A barra inferior mostra os estados da API, do cursor e da fixação. Em janelas estreitas, os painéis ficam em uma coluna com rolagem. O botão **Parada de emergência** executa a mesma ação de **Ctrl + Alt + F10**.

Pare o mouse sobre o **?** ao lado de um campo ou interruptor para ler a dica. Ela fecha quando o mouse sai do ícone. Passe o mouse sobre um botão de ação para ver o que ele faz.

### Configurações salvas

Ao fechar o aplicativo normalmente, o GazeMagnet salva os ajustes de magnetismo e clique, as opções de visualização e os dados de conexão manual em `%LOCALAPPDATA%\GazeMagnet\settings.json`. O Windows protege a chave da API para que só a mesma conta consiga recuperá-la. Ao reabrir, a conexão e o cursor são ativados manualmente. O último modo de clique fica disponível em **Ctrl + Alt + F11**, com o clique desligado na abertura.

### Experimentar sem a câmera

1. Marque **Demonstração com mouse (apenas destaque)**.
2. Aproxime o mouse de um botão, por exemplo **Conectar**. O contorno amarelo tracejado indica um candidato. Ao manter a posição pelo tempo de fixação, o contorno fica verde.
3. Afaste o mouse ou aponte para outro controle. Ajuste raio, fixação e histerese para sentir a diferença. A demonstração mantém o cursor sob seu controle manual.

### Visualizar a área de busca

Em **2. Magnetismo**, marque **Mostrar área de busca e mira**. Com o magnetismo ligado, o círculo azul mostra o raio para encontrar novos alvos. A mira mostra a coordenada usada na busca: o ponto do olhar quando você assume o cursor, ou a posição do mouse no modo de demonstração. Mude **Raio (px)** para aumentar ou reduzir o círculo.

O GazeMagnet procura na janela visível sob a mira. Um controle pode virar candidato se a sua borda alcançar o círculo, mesmo com o centro do controle fora dele. O contorno amarelo continua indicando um candidato e o verde, um alvo fixado. A histerese pode manter um alvo fixado além do círculo.

Enquanto o candidato continuar no círculo, a contagem para fixar segue mesmo que outro botão passe sob a mira. Se o candidato sair do círculo, outro pode começar a contar. Em controles aninhados, mirar diretamente no botão de dentro ou fora dele pode mudar a escolha. Durante uma troca, voltar a mirar no alvo fixado cancela a troca.

A visualização começa desligada. Com o olhar, ela some quando a busca pausa, por exemplo ao perder o sinal, usar o mouse manualmente ou acionar a parada de emergência.

### Estabilizar o olhar

Em **2. Magnetismo**, ajuste **Estabilizar olhar**. O nível **Médio** é o padrão. Use **Leve** se a mira demorar a acompanhar seu olhar, **Forte** se ela ainda tremer enquanto você olha para o mesmo lugar ou **Desligado** para comparar com a leitura original.

A mesma posição suavizada move o cursor livre, orienta a mira e a busca de alvos e define o círculo para o clique pelo olhar. Ao fixar, o cursor ainda vai ao centro do alvo. A demonstração com mouse continua usando a posição do ponteiro.

### Usar com o olhar

Veja a [configuração da porta local e da chave da API](README.md#porta-local-chave-e-reconex%C3%A3o).

1. Abra uma única instância do [GazePointer](https://sourceforge.net/projects/gazepointer/) e calibre o olhar no monitor principal.
2. Na barra inferior do GazePointer, deixe o botão com ícone de ponteiro, **Control Mouse Cursor**, desativado. Mantenha o rastreamento ligado. Desative também o marcador de olhar pelo botão com ícone de olho. Quando esse marcador fica sobre um controle, ele pode encobrir o alvo.
3. No GazeMagnet, desmarque a demonstração. Mantenha **Configurar conexão automaticamente** marcado e clique em **Conectar**. Se o GazePointer mostrar um pedido de acesso, revise-o e autorize a conexão se desejar.
4. Quando aparecer **API conectada · recebendo coordenadas do olhar**, marque a confirmação de calibração e controle nativo desligado. Clique em **Assumir cursor**.
5. Olhe perto de um botão ou link e mantenha o olhar até o contorno ficar verde. O ponteiro vai para o centro do alvo. No modo padrão, entre alvos, o cursor segue as coordenadas recebidas.
6. Para usar o mouse, mova-o normalmente. O GazeMagnet cede o controle durante o movimento e retoma sozinho após cerca de 2 segundos de repouso, assim que chegar uma amostra nova de olhar. Cada novo movimento reinicia a contagem.
7. Use **Pausar cursor** ou **Ctrl + Alt + F10** para voltar ao controle manual. A caixa **Usar magnetismo** permite comparar o cursor pelo olhar com e sem atração.

| Ajuste | Padrão | O que muda |
| --- | ---: | --- |
| Raio | 56 px | Distância máxima da borda do controle para iniciar a atração e contar o tempo do clique. |
| Fixação | 320 ms | Tempo mínimo de permanência no candidato antes de prender. Com a chave de demonstração da API, a baixa frequência pode alongar esse tempo. |
| Histerese | 24 px | Margem extra para soltar o alvo. Um vizinho precisa ficar claramente mais perto e passar pelo tempo de fixação para assumir. |
| Largura máx. | 1600 px | Limite de largura do controle. Linhas e caixas de pesquisa largas podem ser alvos quando estão dentro dos dois limites. |
| Altura máx. | 160 px | Limite de altura do controle. Diminua se áreas altas estiverem atraindo o cursor. |

O GazeMagnet ignora elementos expostos pelo Windows como documentos e agrupadores, como painéis e listas inteiras. Os controles individuais dentro deles continuam disponíveis. Quando o olhar cai sobre um controle menor dentro de outro alvo, o menor tem prioridade depois da fixação. Durante a troca, o clique automático no alvo anterior é cancelado. [Tipos de controle no Windows UI Automation](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-controltypesoverview)

### Mover só entre alvos

Marque **Mover só entre alvos** em **2. Magnetismo**, com **Usar magnetismo** ligado.

- Ao olhar entre controles, o cursor fica na posição atual. Quando surgir um alvo no raio e ele passar pela fixação, o cursor vai direto para o centro dele.
- Enquanto um vizinho aparece em amarelo, o alvo anterior continua em verde se ainda estiver dentro da margem de histerese. Se você mirar dentro do vizinho e fora do anterior, ele pode assumir após a fixação mesmo se estiver muito perto.
- A busca acompanha a janela visível sob o olhar. O clique pelo olhar conta enquanto o controle confirmado estiver dentro do círculo.
- O mouse físico mantém a prioridade. Ao retomar o olhar, o app aguarda uma nova fixação antes de mover o cursor. Desmarque **Mover só entre alvos** para recuperar o movimento livre entre alvos.

### Usar Enter no alvo verde

Em **2. Magnetismo**, marque **Clicar no alvo verde com Enter**. Quando o cursor estiver sobre o alvo verde, pressione **Enter** para dar um clique esquerdo. Segurar a tecla produz um único clique. Fora de um alvo fixado e durante a troca de alvo, Enter continua funcionando normalmente. A opção começa desligada e funciona mesmo com o clique pelo olhar desligado. Dentro do painel do próprio GazeMagnet, Enter mantém sua função normal.

### Clicar pelo olhar

1. Na seção **Clique pelo olhar**, selecione **Treino (sem clique)**. Mantenha **Usar magnetismo** ligado, desmarque a demonstração com mouse e assuma o cursor pelo olhar.
2. Ajuste **Tempo para clicar (s)** de **0,6 a 4,0 segundos**, em passos de 0,1 s. O padrão é **1,0 segundo após a fixação**. Com a fixação padrão de 320 ms, o total nominal é de cerca de 1,3 s. A frequência das amostras e a checagem do alvo podem alongar a espera.
3. Olhe para um botão. Depois que o contorno ficar verde, um preenchimento verde translúcido avança da esquerda para a direita por toda a altura do botão. Um pulso branco sinaliza que o clique foi simulado. Em **Clique esquerdo**, o pulso indica que o clique foi enviado. No painel do próprio GazeMagnet, use cliques manuais.
4. Mantenha o botão verde dentro do círculo para a contagem avançar. Se o botão sair do círculo, a contagem zera mesmo quando a histerese mantém o verde. A troca de alvo, o uso do mouse e a perda de sinal também cancelam a contagem. Depois de um clique, afaste a mira até o botão que recebeu o clique ficar inteiro fora do círculo. O bloqueio protege a região inclusive se um clique abrir um menu ou trocar a tela.
5. Use **Ctrl + Alt + F11** para desligar ou retomar o modo de clique pelo olhar. Se nenhum modo foi escolhido ainda, o atalho abre o treino. **Ctrl + Alt + F10** devolve o cursor ao controle manual e desliga o clique pelo olhar.

O clique pelo olhar começa **desligado** a cada abertura. O clique é enviado como entrada normal do Windows. Algumas janelas sem foco usam o primeiro clique apenas para se ativar, e o Windows pode bloquear o envio para apps com privilégios elevados. [Envio de entrada no Windows](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)

### Recuperação do sinal

Quando o GazePointer perde a posição do olhar por um instante, o GazeMagnet mantém **Pausar cursor** disponível e aguarda o retorno do sinal por até **5 segundos**. O cursor fica parado sob controle do olhar e o mouse físico continua disponível. O destaque e a contagem de clique são cancelados.

Ao receber novas coordenadas válidas durante um breve intervalo, o controle pelo olhar retoma sozinho. O alvo e o clique passam por nova fixação. Se a API continuar conectada mas o sinal do olhar não voltar em cinco segundos, é preciso usar **Assumir cursor** novamente.

### Reconexão automática

Se uma sessão ativa da API cair ou expirar, o GazeMagnet tenta reconectar até três vezes. A conexão pode exigir uma nova autorização no GazePointer. Enquanto espera, o cursor fica manual. Se o controle pelo olhar estava assumido quando a conexão caiu, ele retoma com coordenadas novas e estáveis, respeitando o uso do mouse. Se já estava manual, continua manual.

**Cancelar retomada** ou **Ctrl + Alt + F10** mantém o cursor manual mesmo se a API voltar. **Cancelar reconexão** interrompe as tentativas. Uma autorização recusada ou expirada encerra a tentativa. A reconexão abre uma nova sessão, sujeita aos limites da chave.

## Se algo falhar

- **API indisponível**. Confira se o GazePointer está aberto e se a porta local é 43333. Se a câmera ou a autorização travar, feche o GazePointer por completo, abra novamente e tente conectar. A chave de demonstração do fornecedor pode ter limite de tempo e frequência.
- **Cursor voltou ao modo manual**. Uma perda do sinal por 5 segundos ou uma reconexão sem sucesso devolve o cursor ao modo manual. Confira o rastreamento e o botão **Control Mouse Cursor** antes de rearmar. O GazeMagnet exige o atalho de emergência disponível para assumir o cursor.
- **Um controle fica sem destaque**. A detecção depende dos controles expostos ao Windows UI Automation pela janela visível sob o olhar. Ela pode estar sem foco. A seleção exige que o centro do controle esteja visível e não ativa a janela. Jogos, interfaces desenhadas em canvas, apps com privilégios elevados e alguns controles personalizados podem ficar fora da cobertura.
- **As coordenadas parecem deslocadas**. O MVP usa coordenadas em pixels do monitor principal. Valide a calibração nessa tela. Escalas diferentes entre monitores ainda precisam de validação.

O cliente usa TCP local em `127.0.0.1`, conforme o [exemplo do GazeFlowAPI](https://github.com/szydej/GazeFlowAPI/blob/master/GazeFlowAPI.cs), e não grava coordenadas. O GazePointer pode consultar o serviço de autorização do próprio fornecedor. O fluxo legado que testei entrega coordenadas, mas não informa o estado do rastreador. O GazeMagnet pausa quando as amostras param, ficam inválidas ou quando o quadro recebido se repete sem mudança. Se o rastreador continuar emitindo valores plausíveis mesmo após perder o rosto, use o atalho de emergência.

## Compilar

Instale o [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) para Windows. No PowerShell, a partir desta pasta, execute:

```powershell
dotnet build .\src\GazeMagnet.csproj -c Release
dotnet run --project .\src\GazeMagnet.csproj
```

## O que foi verificado

- Compilação Release sem erros e sem avisos.
- Conexão com o GazePointer instalado e recebimento de coordenadas pela API local.
- Com o GazePointer fechado, mensagem de API indisponível e cursor em modo manual.
- Atalho de emergência verificado na interface com a demonstração ativa.
- Fixação, troca de alvo, histerese, liberação, pacotes fragmentados e quadros inválidos ou congelados verificados com dados controlados.
- Detecção de um botão real via Windows UI Automation e verificação de que o destaque aparece sem tomar foco nem interceptar o clique.
- Na versão 0.2.0, o scanner localizou um botão visível em uma janela sem foco e descartou o botão encoberto. O destaque preservou o foco da janela ativa.
- Com movimentos e amostras simulados, a prioridade manual foi renovada a cada movimento e o controle por olhar pôde retomar sem rearmar.
- Com coordenadas simuladas, a versão 0.3.0 respeitou o tempo ajustado, cancelou ao desviar, executou o treino sem clicar e enviou um único clique para um botão real do Windows. Manter o olhar simulado na mesma região não disparou outro clique.
- Na versão 0.4.0, uma página de teste no Chrome confirmou que o scanner localiza botões e linhas largas dentro de um grupo, ignora o grupo e exclui controles altos até que o limite seja aumentado. A prioridade para um controle menor dentro de um alvo já fixado foi verificada com dados controlados.
- Na versão 0.5.0, coordenadas simuladas em dois botões reais verificaram que o cursor permanece parado em área vazia, vai direto de um botão ao outro sem clicar no anterior, dá prioridade ao mouse e volta a seguir o olhar quando o modo é desativado.
- Na versão 0.6.0, perdas breves simuladas mantiveram o cursor parado, cancelaram o clique pendente e retomaram o olhar após novas amostras válidas. Uma interrupção prolongada exigiu novo acionamento manual.
- Na versão 0.7.0, o teste de interface conferiu valores e limites, interruptores, estados operacionais, botão de emergência, layout largo e estreito e notificações simuladas de escala a 125%, 150% e 200%.
- Na versão 0.7.6, a captura de tela confirmou o círculo no raio configurado e a mira na posição de busca. O teste cobriu a demonstração com mouse, amostras simuladas de olhar, perda de sinal e parada de emergência. Com a mira ativa, um botão de teste recebeu um único clique pelo olhar.
- Na versão 0.7.7, amostras simuladas a cada 33 ms e 200 ms confirmaram a redução de oscilação nos três níveis. A interface foi testada com a mira ativa, o movimento do cursor e o cancelamento da contagem de clique ao desviar a leitura original.
- Na versão 0.7.8, testes com coordenadas simuladas verificaram a fixação sem reinício quando a mira oscila entre botões e o candidato continua dentro do raio. Os testes de interface também verificaram a exibição simultânea dos dois contornos e a retenção pela histerese no modo **Mover só entre alvos**.
- Na versão 0.7.9, testes com coordenadas simuladas confirmaram a contagem dentro do círculo, o cancelamento ao sair e o bloqueio de repetição. Com a mira fora da borda do botão e o botão dentro do círculo, um botão de teste recebeu um único clique no Windows.
- Na versão 0.7.10, a captura no Windows confirmou o preenchimento verde translúcido por toda a altura do botão. O teste verificou o preenchimento completo no disparo, a remoção ao cancelar e a passagem do clique.
- Na versão 0.7.11, com coordenadas simuladas, um botão real recebeu foco e foi acionado com Enter. Shift + Tab moveu o foco normalmente enquanto o botão permaneceu verde. Um alvo em outra janela ficou sem foco até sua janela ficar ativa.
- Na versão 0.7.12, o clique com Enter substituiu o foco automático. O teste no Windows confirmou um clique por pressão, inclusive no Enter numérico, e o Enter normal quando a opção está desligada ou não há alvo verde.
- Na versão 0.7.13, uma API local de teste simulou expiração, fechamento e interrupção no envio de dados. Os testes verificaram a retomada com sinal novo, a prioridade do mouse, o modo manual, a parada de emergência, o cancelamento da conexão, a recusa de acesso e o limite de três tentativas.
- O aviso vermelho com a webcam ainda precisa de validação presencial.
