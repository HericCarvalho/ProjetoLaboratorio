# Neuroevolucao - Melhoria do Laboratorio

Projeto desenvolvido no Unity 6 a partir da base da Aula 05, adicionando rota de referencia com A*, metricas de navegacao, validacao em tres cenarios e persistencia do melhor genoma via PlayerPrefs.

## Como abrir e usar a cena

1. Abra o projeto no Unity 6 (`Assets/Scenes/Vortex.unity`).
2. Se precisar reconstruir a cena, clique no menu `Vortex -> Montar Cena Neuroevolucao`.
3. Pressione Play. A evolucao inicia automaticamente no Cenario 1 usando a seed `12345`.
4. Use os botoes na tela (ou teclas `S`, `L`, `D`) para:
   - **Salvar melhor (S):** salva o melhor genoma atual em `PlayerPrefs` (`neuroevolution.best.v1`).
   - **Carregar melhor (L):** carrega o genoma salvo e executa em modo de validacao, sem evoluir novamente.
   - **Apagar save (D):** remove a chave salva.
   - **Cenario 1 / 2 / 3:** alterna entre o cenario de treino e os dois cenarios de validacao.

## Video demonstrativo

[Video_Final_Neuroevolucao.mp4](Video_Final_Neuroevolucao.mp4)

## Formula do fitness

`fitness = (distanciaInicial - distanciaAtualAoGoal) * 10 + bonusChegada(100) - penalidadeColisao(50) - penalidadeTempo(1)`

- `ProgressWeight = 10`
- `GoalBonus = 100`
- `CollisionPenalty = -50`
- `TimePenalty = 1`
- Seed utilizada: `12345` (`populationSize = 30`, `mutationRate = 0.08`, `mutationStrength = 0.25`)

## Tabela dos tres cenarios (Validacao)

| Cenario | Chegou ao alvo | Colidiu | Tempo (s) | Distancia real (m) | Distancia referencia A* (m) | Eficiencia |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| Cenario 1 (Treino) | Sim | Nao | 4.20 | 26.85 | 25.60 | 0.953 |
| Cenario 2 (Validacao) | Sim | Nao | 4.65 | 29.10 | 26.95 | 0.926 |
| Cenario 3 (Validacao) | Sim | Nao | 5.10 | 31.40 | 28.20 | 0.898 |

## Parte D - Analise

1. **O melhor fitness correspondeu a rota mais eficiente?**
Nem sempre na mesma geracao. Em alguns episodios uma nave com fitness alto fazia uma curva maior para desviar com folga dos obstaculos e chegava ao alvo somando o bonus de +100, ficando com fitness maior do que outra que tentou um caminho mais curto e acabou batendo antes de chegar (levando -50). Quando ambas chegam ao alvo, a mais rapida tende a ter distancia real menor e eficiencia mais proxima de 1.

2. **Em qual cenario de validacao o agente teve pior desempenho? Por que?**
No Cenario 3, porque a posicao dos obstaculos e do Goal mudou para o lado oposto ao do treino, exigindo uma curva diferente logo na saida. Como a rede foi treinada apenas no Cenario 1, ela demorou um pouco mais para corrigir a trajetoria pelos sensores.

3. **Qual alteracao no fitness poderia induzir um comportamento indesejado?**
Se aumentassemos muito o `ProgressWeight` sem penalidade de colisao alta ou sem limitar o progresso, a nave poderia aprender a acelerar reto em direcao ao alvo o mais rapido possivel e bater de frente no obstaculo no meio do caminho, porque o ganho inicial de distancia compensaria a batida. Outro exemplo seria remover a exigencia de progresso e dar pontos so por tempo vivo, fazendo a nave girar em circulos sem sair do lugar.

4. **O genoma salvo reproduziu o comportamento esperado apos fechar e reabrir a cena?**
Sim. Usando a seed `12345`, apos salvar o melhor genoma na geracao evoluida e reabrir a cena no modo Play clicando em Carregar melhor (`L`), a nave seguiu exatamente a mesma trajetoria ate o Goal sem precisar treinar novamente.
