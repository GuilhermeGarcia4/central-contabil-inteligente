using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Infrastructure;

/// <summary>Provisiona conteúdo público revisado sem alterar artigos que já existem.</summary>
public sealed class FinancialEducationSeed(AppDbContext db, ILogger<FinancialEducationSeed> logger)
{
    private static readonly SourceDefinition[] Sources =
    [
        new("Tributação de altas rendas — Receita Federal", "https://www.gov.br/receitafederal/pt-br/assuntos/noticias/2025/dezembro/receita-federal-atualiza-normas-relativas-ao-imposto-sobre-a-renda-das-pessoas-fisicas", "Receita Federal", SourceType.GovernmentPublication),
        new("Perguntas e respostas sobre tributação de altas rendas — Receita Federal", "https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/perguntas-e-respostas/dirf/manual_padrao_rfb_per_tributacao_cotin_v-19-12-2025.pdf", "Receita Federal", SourceType.GovernmentPublication),
        new("IRPF — Perguntas e respostas", "https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/perguntas-e-respostas/dirpf", "Receita Federal", SourceType.OfficialPortal),
        new("Tributação de 2026", "https://www.gov.br/receitafederal/pt-br/assuntos/meu-imposto-de-renda/tabelas/2026", "Receita Federal", SourceType.OfficialPortal),
        new("Portabilidade de crédito — como solicitar", "https://www.bcb.gov.br/meubc/faqs/p/como-solicitar-a-portabilidade-de-credito", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("Saiba como fazer a portabilidade de crédito", "https://www.bcb.gov.br/detalhenoticia/206/noticia", "Banco Central do Brasil", SourceType.GovernmentPublication),
        new("Administradoras de consórcio", "https://www.bcb.gov.br/estabilidadefinanceira/administradoraconsorcio", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("Perguntas frequentes sobre consórcio", "https://www.bcb.gov.br/meubc/faqs/s/consorcio", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("Empréstimos e financiamentos — perguntas frequentes", "https://www.bcb.gov.br/meubc/faqs/s/emprestimos-e-financiamentos", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("Tipos de empréstimos disponíveis", "https://www.bcb.gov.br/detalhenoticia/227/noticia", "Banco Central do Brasil", SourceType.GovernmentPublication),
        new("Caderno de educação financeira — gestão das finanças pessoais", "https://www.bcb.gov.br/content/cidadaniafinanceira/documentos_cidadania/cuidando_do_seu_dinheiro_gestao_de_Financas_Pessoais/caderno_cidadania_financeira.pdf", "Banco Central do Brasil", SourceType.GovernmentPublication),
        new("Lidando com imprevistos e reserva de emergência", "https://www.bcb.gov.br/cidadaniafinanceira/lidando-imprevistos", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("IOF em investimentos — Portal do Investidor", "https://www.gov.br/investidor/pt-br/educacional/publicacoes-educacionais/livros-cvm/livro-top-planejamento-financeiro-pessoal/", "Comissão de Valores Mobiliários", SourceType.GovernmentPublication),
        new("Compra de títulos públicos — tributação", "https://www.gov.br/tesouronacional/pt-br/perguntas-frequentes/divida-publica/compra-de-titulos-publicos", "Tesouro Nacional", SourceType.OfficialPortal),
        new("Constituição Federal — competências tributárias", "https://www.planalto.gov.br/ccivil_03/constituicao/constituicaocompilado.htm", "Presidência da República", SourceType.Law),
        new("Meu Imposto de Renda", "https://www.gov.br/receitafederal/pt-br/assuntos/meu-imposto-de-renda", "Receita Federal", SourceType.OfficialPortal),
        new("Cadastro Único", "https://www.gov.br/mds/pt-br/acoes-e-programas/cadastro-unico/Cadastro-unico", "Ministério do Desenvolvimento e Assistência Social", SourceType.OfficialPortal),
        new("Bolsa Família — receber o benefício", "https://www.gov.br/pt-br/servicos/receber-o-bolsa-familia", "Governo Federal", SourceType.OfficialPortal),
        new("Benefício de Prestação Continuada — BPC", "https://www.gov.br/mds/pt-br/acoes-e-programas/beneficio-de-prestacao-continuada-bpc", "Ministério do Desenvolvimento e Assistência Social", SourceType.OfficialPortal),
        new("Portal do Empreendedor", "https://www.gov.br/empresas-e-negocios/pt-br/empreendedor", "Ministério do Empreendedorismo, da Microempresa e da Empresa de Pequeno Porte", SourceType.OfficialPortal),
        new("Perguntas e respostas sobre o MEI — Simples Nacional", "https://www8.receita.fazenda.gov.br/SimplesNacional/Arquivos/manual/PerguntaoMEI.pdf", "Receita Federal", SourceType.GovernmentPublication),
        new("Microempreendedor Individual — direitos previdenciários", "https://www.gov.br/inss/pt-br/saiba-mais/seus-direitos-e-deveres/categorias-de-segurados/microempreendedor-individual", "Instituto Nacional do Seguro Social", SourceType.OfficialPortal),
        new("Cartão de crédito — perguntas frequentes", "https://www.bcb.gov.br/meubc/faqs/s/cartao", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("Limite de juros e encargos no cartão", "https://www.bcb.gov.br/meubc/faqs/p/limitacao-dos-juros-e-encargos-financeiros-no-saldo-devedor-da-fatura-do-cartao", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("Cadastro Positivo — perguntas frequentes", "https://www.bcb.gov.br/meubc/faqs/s/cadastro-positivo", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("Registrato", "https://www.bcb.gov.br/cidadaniafinanceira/registrato", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("FGTS — saques", "https://www.fgts.gov.br/Paginas/Trabalhador/saque.aspx", "Fundo de Garantia do Tempo de Serviço", SourceType.OfficialPortal),
        new("Contribuição previdenciária e salário de contribuição", "https://www.gov.br/inss/pt-br/direitos-e-deveres/inscricao-e-contribuicao/contribuicao-previdenciaria-e-salario-de-contribuicao", "Instituto Nacional do Seguro Social", SourceType.OfficialPortal),
        new("Golpes — como evitar e o que fazer", "https://www.bcb.gov.br/meubc/faqs/s/golpes", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("Segurança no Pix", "https://www.bcb.gov.br/estabilidadefinanceira/pix-seguranca", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("Superendividamento", "https://www.gov.br/mj/pt-br/assuntos/seus-direitos/consumidor/defesadoconsumidor/Superendividamento", "Ministério da Justiça e Segurança Pública", SourceType.OfficialPortal),
        new("Consumidor.gov.br — legislação de proteção do consumidor", "https://www.consumidor.gov.br/pages/conteudo/publico/102", "Secretaria Nacional do Consumidor", SourceType.OfficialPortal),
        new("Aprender Valor — educação financeira para crianças e jovens", "https://www.bcb.gov.br/cidadaniafinanceira/ensinando-criancas-adolescentes", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("Aprender Valor — perguntas frequentes", "https://www.bcb.gov.br/meubc/faqs/s/aprender-valor", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("Taxas de juros médias por modalidade", "https://www.bcb.gov.br/estatisticas/txjuros", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("IPVA — orientações da Secretaria da Fazenda de Pernambuco", "https://www.sefaz.pe.gov.br/Servicos/IPVA/Paginas/Perguntas-e-Respostas.aspx", "Secretaria da Fazenda de Pernambuco", SourceType.OfficialPortal),
        new("compound-interest-bcb", "Calculadora do Cidadão — juros compostos", "https://www.bcb.gov.br/meubc/calculadoradocidadao", "Banco Central do Brasil", SourceType.OfficialPortal),
        new("compound-interest-learn", "Aprender Valor — fórmula de juros compostos", "https://www.bcb.gov.br/content/config/aprendervalor/docs_aprender_valor/OLITEF_2024_CADERNO_ESTUDANTE_VFinal.pdf", "Banco Central do Brasil", SourceType.GovernmentPublication)
    ];

    private static readonly ArticleDefinition[] Articles =
    [
        new("Tributação de altas rendas", "tributacao-altas-rendas", "Entenda as mudanças recentes do Imposto de Renda, a retenção sobre dividendos e o que precisa ser analisado em cada situação.", "contabilidade", ["high-income", "high-income-qa", "irpf-faq"],
        """
        ## O que mudou
        A Lei nº 15.270/2025 alterou regras do Imposto de Renda. A partir de 2026 há retenção na fonte em situações específicas de distribuição de lucros e dividendos a pessoas físicas residentes. A tributação mínima anual para altas rendas considera rendimentos do ano-calendário de 2026 e será apurada na declaração do exercício de 2027.

        ## Exemplo do cotidiano
        Uma pessoa que recebe salário, aluguel, aplicações e lucros de uma empresa deve considerar cada origem e as regras próprias. A expressão “alta renda” não significa que todo pagamento ou todo salário passe automaticamente a ter uma nova alíquota: a incidência depende do tipo, valor, período e origem do rendimento.

        ## Pontos de atenção
        A regra mensal de dividendos observa pagamentos feitos por uma mesma pessoa jurídica à mesma pessoa física. Há regras de transição e exceções para lucros apurados e aprovados em períodos anteriores. Não some rendimentos de naturezas diferentes sem conferir o cálculo da lei e as orientações da Receita.

        ## Como se organizar
        Mantenha informes de rendimentos, atas de distribuição, comprovantes de retenção e registros contábeis. Em distribuição de lucros ou renda de várias fontes, peça uma análise individual a profissional habilitado e consulte a versão atual das perguntas e respostas da Receita.

        ## Perguntas frequentes
        P: A regra trata de todo salário acima de um valor mensal?
        R: Não. As regras recentes incluem mecanismos distintos para redução do IRPF e tributação de dividendos e altas rendas. A incidência depende da natureza do rendimento e do cálculo legal.

        P: Quando começa a apuração anual de altas rendas?
        R: A Receita informa que ela será aplicada na declaração de 2027, referente ao ano-calendário de 2026.

        ## Conteúdos relacionados
        Imposto de Renda sem complicação; IOF e rendimentos; MEI sem complicação.
        """,
        """
        ## Escopo da regra
        A Lei nº 15.270/2025 instituiu redução do imposto em bases mensal e anual e tributação mínima anual para pessoas físicas com rendimentos acima do limite legal. Também prevê retenção de IRRF sobre determinadas distribuições de lucros e dividendos. Para pessoa física residente, a Receita descreve retenção de 10% quando uma mesma pessoa jurídica paga à mesma pessoa mais de R$ 50 mil no mês; o cálculo incide sobre o total do pagamento nos termos legais. A regra anual de altas rendas é aplicável no exercício de 2027, ano-calendário 2026.

        ## Verificações antes de decidir
        Diferencie pessoa pagadora, beneficiário, natureza do rendimento, competência e regras de transição. Distribuições aprovadas dentro dos prazos e requisitos legais podem receber tratamento específico. A legislação e as instruções administrativas podem ser atualizadas; use a Receita Federal e o texto consolidado da lei como referência.

        ## Limite desta explicação
        Este resumo não calcula imposto nem substitui análise tributária e societária. Distribuições de lucros exigem escrituração e deliberações válidas. Consulte contador ou advogado tributarista para enquadramento concreto.
        """),

        new("Portabilidade de dívidas", "portabilidade-de-dividas", "Saiba como transferir uma operação de crédito e comparar o custo total da proposta nova com o contrato atual.", "financeiro", ["portability-faq", "portability-guide", "loan-faq"],
        """
        ## O que é
        Portabilidade de crédito é a transferência de uma dívida para outra instituição que aceita quitá-la e oferecer um novo contrato. O processo deve ser iniciado com a instituição proponente. A portabilidade não apaga a dívida: troca o credor e as condições contratadas.

        ## Exemplo do cotidiano
        Antes de aceitar uma oferta recebida por telefone, peça ao credor atual o saldo para quitação e o Documento Descritivo do Crédito. Solicite à nova instituição o CET, prazo, parcelas, sistema de amortização, tarifas e seguros. Compare o total restante no contrato atual com o custo total da nova operação.

        ## Benefícios e riscos
        Uma taxa menor pode reduzir o custo, mas prazo maior, tarifas ou serviços agregados podem elevar o total pago. A parcela menor, sozinha, não prova que a proposta seja mais barata. A instituição nova também pode recusar a concessão de crédito após análise.

        ## Como se proteger
        Faça o pedido pelos canais oficiais, não pague intermediário para “liberar” portabilidade e não entregue senha ou código de autenticação. Guarde protocolos e confira se a operação foi liquidada no credor original.

        ## Perguntas frequentes
        P: A instituição antiga pode cobrar uma taxa pela transferência entre bancos?
        R: O Banco Central informa que custos de comunicação entre as instituições para a portabilidade não podem ser repassados ao devedor.

        P: O que devo comparar?
        R: CET, saldo financiado, prazo, valor total, parcelas, seguros e demais encargos.

        ## Conteúdos relacionados
        Empréstimos; juros; endividamento e renegociação.
        """,
        """
        ## Etapas e dados
        O consumidor solicita a portabilidade à instituição proponente. Para encaminhar a requisição, são compartilhados dados da operação original, incluindo taxa nominal e efetiva anual, CET, prazo, sistema de amortização e valor das prestações. A instituição credora deve fornecer informações do saldo e da evolução da dívida para permitir a comparação.

        ## Comparação econômica
        Compare fluxos equivalentes na mesma data: saldo para liquidação antecipada e pagamentos vincendos do contrato atual versus principal, encargos e pagamentos do contrato proposto. Inclua custos que não estejam no CET quando forem opcionais ou condicionados, e verifique se a proposta incorpora dinheiro adicional (troco), que altera o principal.

        ## Segurança
        Não autorize uma contratação diferente da portabilidade sem avaliar o novo principal e o custo. Formalize desistência se mudar de ideia e confirme a quitação com a instituição original. A norma e o procedimento variam por modalidade; consulte o Banco Central para a operação específica.
        """),

        new("Consórcio ou financiamento", "consorcio-ou-financiamento", "Compare tempo de acesso ao bem, encargos, reajustes e riscos antes de escolher entre consórcio e financiamento.", "financeiro", ["consortium", "consortium-faq", "loan-faq"],
        """
        ## Como cada opção funciona
        No financiamento, uma instituição concede crédito para adquirir um bem ou serviço e o cliente paga parcelas com juros e outros encargos. No consórcio, participantes contribuem para um grupo de autofinanciamento administrado por empresa autorizada; o crédito é liberado quando há contemplação por sorteio ou lance, conforme contrato e recursos do grupo.

        ## Comparação prática
        **Financiamento:** pode permitir acesso imediato após aprovação e pagamento da entrada. O custo inclui juros, tributos, tarifas e seguros; o bem pode servir de garantia.

        **Consórcio:** não há juros de financiamento sobre o crédito como nessa modalidade, mas existem taxa de administração e outros custos previstos no contrato. Não há data garantida de contemplação. O crédito e as parcelas podem ser atualizados conforme o contrato.

        ## Exemplo do cotidiano
        Se você precisa do carro para trabalhar já, o prazo incerto de contemplação pode inviabilizar um consórcio. Se pode esperar e compreende reajustes, um grupo pode ser avaliado comparando taxa de administração, fundo de reserva, seguros e prazo.

        ## Perguntas frequentes
        P: Pagar parcelas adiantadas garante contemplação?
        R: Não. O Banco Central informa que a contemplação ocorre por sorteio ou lance e depende dos recursos do grupo.

        P: Qual opção é sempre mais barata?
        R: Nenhuma é sempre melhor. Compare custo total, prazo de acesso, reajustes, garantias e capacidade de pagamento.

        ## Conteúdos relacionados
        Empréstimos; juros; planejamento financeiro familiar.
        """,
        """
        ## Critérios de comparação
        Para financiamento, some entrada, parcelas, tarifas, tributos, seguros e despesas de registro. Verifique CET e regras de amortização antecipada. Para consórcio, considere fundo comum, taxa de administração, fundo de reserva, seguro, reajuste do crédito e das prestações, regras de lance, prazo e devolução em caso de exclusão.

        ## Contemplação e contrato
        A contemplação não é imediata nem garantida por antecipação de parcelas; ocorre por sorteio ou lance segundo o regulamento e a disponibilidade do grupo. Depois de contemplado, o consorciado ainda pode precisar demonstrar capacidade de pagamento e cumprir garantias previstas. A administradora deve ser autorizada pelo Banco Central.

        ## Decisão
        Compare cenários na mesma data e não trate parcela inicial como custo fixo quando o contrato permite atualização. Leia o contrato integral, a política de desistência e os efeitos de atraso.
        """),

        new("Empréstimos: entenda antes de contratar", "emprestimos-antes-de-contratar", "Conheça modalidades de crédito e aprenda a comparar CET, prazo, garantias e parcelas sem olhar apenas a taxa anunciada.", "financeiro", ["loan-faq", "loan-types", "interest-basics"],
        """
        ## O que avaliar
        Empréstimo é crédito recebido agora e devolvido em pagamentos futuros. Crédito pessoal, consignado e empréstimo com garantia têm requisitos, riscos e custos diferentes. Taxas variam conforme instituição, modalidade, perfil, garantia e prazo; uma taxa divulgada como média não é uma oferta individual.

        ## Exemplo do cotidiano
        Duas propostas podem anunciar juros mensais parecidos, mas ter prazos e tarifas diferentes. Peça o CET mensal e anual, o valor líquido recebido, quantidade e valor de parcelas e total a pagar. Compare as propostas com o mesmo valor e prazo.

        ## Riscos e cuidados
        Uma parcela que cabe hoje pode comprometer despesas essenciais se a renda cair. Em crédito com garantia, o atraso pode levar à execução do bem. No consignado, o desconto reduz diretamente a renda disponível. Nunca aceite contratação condicionada a pagamento antecipado para liberar o dinheiro.

        ## Antes de assinar
        Defina a necessidade, avalie alternativas, confira se a instituição é autorizada, leia o contrato e guarde uma cópia. Planeje a parcela no orçamento e verifique regras de quitação antecipada.

        ## Perguntas frequentes
        P: Devo comparar somente a taxa de juros?
        R: Não. O CET reúne juros, tarifas, tributos e outros custos da operação.

        P: O banco pode cobrar qualquer taxa anunciada?
        R: As condições dependem da modalidade e da negociação; peça os dados da proposta e confira as regras específicas.

        ## Conteúdos relacionados
        Portabilidade de dívidas; cartão de crédito; endividamento e renegociação.
        """,
        """
        ## Leitura da proposta
        Exija a taxa efetiva mensal e anual, CET, tributos, tarifas, seguros, valor líquido creditado, sistema de amortização, vencimentos e encargos por atraso. O CET deve ser apresentado antes da contratação e permite comparar operações com custos acessórios. As taxas médias do Banco Central são referência estatística por modalidade e período, não promessa de taxa individual.

        ## Capacidade de pagamento
        Insira a prestação no orçamento incluindo dívidas existentes, despesas irregulares e uma margem para imprevistos. Simule queda de renda e aumento de despesas. Uma garantia pode reduzir a taxa oferecida, mas expõe o bem ao risco de execução em caso de inadimplência.

        ## Direitos e prevenção a fraude
        Solicite cópia do contrato e Documento Descritivo do Crédito. Contratação não exige depósito antecipado em conta de terceiros. Use canais oficiais da instituição e registre reclamação nos canais de defesa do consumidor quando necessário.
        """),

        new("IOF e rendimentos", "iof-e-rendimentos", "Entenda como prazo, produto e tributos podem alterar o rendimento líquido de uma aplicação financeira.", "financeiro", ["investment-taxes", "treasury-tax", "irpf-faq"],
        """
        ## Rendimento bruto e líquido
        A rentabilidade divulgada antes de custos não é necessariamente o valor que chega ao investidor. Dependendo do produto, podem incidir Imposto de Renda, IOF, taxas de administração, custódia ou outros custos. As regras mudam conforme tipo de aplicação, prazo e forma de resgate.

        ## Exemplo do cotidiano
        Em determinados investimentos de renda fixa, resgates em menos de 30 dias podem ter IOF sobre o rendimento, com alíquota regressiva por prazo. Isso não significa que toda aplicação tenha a mesma regra: produtos e regimes tributários têm tratamentos diferentes. Verifique o documento do produto e a regra vigente.

        ## Como comparar
        Compare o valor líquido projetado, prazo de carência, liquidez, risco de crédito, oscilação de preço e cobertura aplicável. Uma taxa maior pode vir acompanhada de prazo de bloqueio ou risco maior.

        ## Perguntas frequentes
        P: O IOF incide sobre todo o saldo investido?
        R: Nas aplicações de renda fixa descritas nas fontes oficiais, o IOF de resgate de curto prazo incide sobre o rendimento, conforme a regra aplicável.

        P: Depois de 30 dias nenhum imposto é cobrado?
        R: Não se pode generalizar. Em certos produtos deixa de haver IOF sobre o resgate, mas pode haver IR ou outros custos.

        ## Conteúdos relacionados
        Imposto de Renda; juros; reserva de emergência.
        """,
        """
        ## IOF e IR não são a mesma coisa
        O IOF pode incidir em operações de crédito, câmbio, seguro e em situações determinadas de títulos e valores mobiliários. Em aplicações de renda fixa, o resgate antes de 30 dias pode gerar IOF sobre o rendimento, com limite regressivo conforme o prazo. O tratamento depende do instrumento e das regras em vigor.

        ## Apuração do retorno
        Para comparar produtos, identifique remuneração bruta, indexador, período, custos, tributação, liquidez e risco. Em renda fixa tributável, o IR pode ser retido na fonte e sua alíquota depende do regime e do prazo. Produtos isentos para determinada pessoa investidora não devem ser tratados como isentos em qualquer situação.

        ## Limite
        Este artigo não estima rentabilidade nem indica investimento. Confira lâmina, prospecto, regulamento e informe de rendimentos do produto; para valores atuais, consulte Receita Federal, Tesouro Nacional ou CVM.
        """),

        new("IPTU e IPVA", "iptu-e-ipva", "Saiba por que calendário, alíquotas, descontos e isenções de IPTU e IPVA dependem do local e da legislação aplicável.", "contabilidade", ["constitution-taxes", "ipva-pe", "irpf-faq"],
        """
        ## Dois tributos diferentes
        IPTU incide sobre propriedade predial e territorial urbana e é instituído pelo município. IPVA incide sobre propriedade de veículos automotores e é administrado pelos estados e pelo Distrito Federal, conforme a legislação aplicável.

        ## Exemplo do cotidiano
        O vencimento do IPTU de um imóvel em uma cidade pode ser diferente do calendário de IPVA do estado onde um veículo está registrado. Descontos por cota única, parcelamento, critérios de cálculo e hipóteses de isenção também podem variar.

        ## Como evitar erros
        Consulte o carnê e o portal oficial da prefeitura para IPTU e o portal da Secretaria da Fazenda do estado para IPVA. Confira placa ou cadastro do imóvel, exercício, código de barras, datas, descontos e regras de contestação. Não use calendários de outros estados ou municípios.

        ## Perguntas frequentes
        P: Existe um calendário nacional único?
        R: Não. O calendário e as regras de pagamento dependem do ente competente.

        P: O pagamento atrasado gera custo adicional?
        R: A legislação local pode prever multa, juros e atualização. Consulte o documento oficial da cobrança.

        ## Conteúdos relacionados
        Imposto de Renda; organização financeira pessoal; planejamento familiar.
        """,
        """
        ## Competência e variação local
        A Constituição Federal atribui aos municípios competência sobre o IPTU e aos estados e ao Distrito Federal competência sobre o IPVA. As normas locais definem base de cálculo, alíquotas, vencimentos, descontos, parcelamento, isenções e meios de impugnação, respeitados os limites constitucionais e legais.

        ## Conferência da cobrança
        Verifique exercício, cadastro imobiliário ou registro do veículo, valor venal/base utilizada, alíquota aplicável, benefícios fiscais e protocolo de contestação. Mudança de município ou de estado pode alterar órgão arrecadador e prazos. A página da Secretaria da Fazenda de Pernambuco é apenas um exemplo de fonte estadual, não uma regra nacional.

        ## Atualização
        Antes de pagar, consulte o portal do município ou da Secretaria da Fazenda responsável pelo cadastro. Este resumo não calcula tributo nem substitui consulta à legislação local.
        """),

        new("Imposto de Renda sem complicação", "imposto-de-renda-sem-complicacao", "Organize documentos e entenda a diferença entre declarar, pagar imposto e receber restituição.", "contabilidade", ["irpf-faq", "irpf-2026", "high-income"],
        """
        ## Declaração e imposto devido
        A declaração anual reúne rendimentos, bens, dívidas, pagamentos e informações familiares do período definido pela Receita. Estar obrigado a declarar não significa necessariamente ter imposto a pagar; a declaração também pode apurar restituição ou saldo zero. Critérios e limites mudam por exercício.

        ## Exemplo do cotidiano
        Uma pessoa pode receber salário, aluguel e rendimentos financeiros. Reúna informes de todas as fontes, recibos, comprovantes de despesas dedutíveis, dados de dependentes e documentos de bens. A declaração pré-preenchida ajuda, mas o contribuinte precisa conferir e corrigir informações.

        ## Dicas
        Identifique o exercício e o ano-calendário corretos, guarde comprovantes, verifique dados bancários para restituição e acompanhe pendências no portal oficial. Não entregue senha gov.br a terceiros nem envie documentos por links recebidos sem conferir o domínio.

        ## Perguntas frequentes
        P: Preciso declarar todo ano?
        R: A obrigação depende dos critérios publicados pela Receita para cada exercício. Consulte a página daquele ano.

        P: A pré-preenchida garante que está tudo certo?
        R: Não. O contribuinte deve revisar valores, fontes e deduções e manter documentos comprobatórios.

        ## Conteúdos relacionados
        Tributação de altas rendas; IOF e rendimentos; MEI sem complicação.
        """,
        """
        ## Exercício, calendário e apuração
        A declaração do exercício 2026 informa fatos do ano-calendário 2025. A Receita publica regras de obrigatoriedade, tabelas, prazos e perguntas e respostas para cada exercício. Rendimentos tributáveis, isentos, exclusivos, patrimônio, ganhos de capital e operações no exterior têm campos e tratamentos próprios.

        ## Documentação e retificação
        Reconcilie os informes de fontes pagadoras e instituições financeiras com seus registros. Guarde recibo e comprovantes pelo prazo legal. Se encontrar erro depois da transmissão, consulte a orientação oficial para retificar, observando restrições do processamento e da restituição.

        ## Proteção de dados
        Use o serviço oficial da Receita e autenticação gov.br. Não forneça credenciais a intermediários e revise procurações eletrônicas. Situações com renda no exterior, atividade empresarial, espólio ou múltiplas fontes podem exigir orientação profissional.
        """),

        new("Benefícios do Governo", "beneficios-do-governo", "Conheça os canais oficiais para identificar benefícios sociais e manter seus dados cadastrais atualizados.", "trabalhista", ["cadunico", "bolsa-family", "benefits-bpc"],
        """
        ## Onde começar
        O Cadastro Único é uma das portas de entrada para seleção de famílias em diversos programas federais, mas o cadastro não garante concessão automática. Cada benefício tem critérios próprios de renda, composição familiar, idade, deficiência, contribuição ou situação social.

        ## Exemplo do cotidiano
        Uma família que mudou de endereço, teve alteração de renda ou composição deve atualizar o Cadastro Único no canal indicado pelo município. A informação correta ajuda a administração a avaliar o perfil e pode evitar bloqueios decorrentes de cadastro desatualizado.

        ## Como verificar
        Consulte páginas oficiais do MDS, INSS, Caixa e prefeitura. Confirme requisitos, documentos, prazos e forma de acompanhamento. Desconfie de quem promete aprovação ou cobra taxa para liberar benefício: use canais governamentais.

        ## Perguntas frequentes
        P: Ter Cadastro Único garante receber Bolsa Família ou outro programa?
        R: Não. O cadastro subsidia a seleção, mas cada programa aplica suas regras e disponibilidade.

        P: Onde atualizo meus dados?
        R: Consulte o aplicativo/site oficial e o posto do Cadastro Único ou CRAS do município.

        ## Conteúdos relacionados
        FGTS e INSS; MEI sem complicação; planejamento financeiro familiar.
        """,
        """
        ## Elegibilidade e manutenção
        O Cadastro Único reúne dados declarados e verificados para orientar políticas sociais. A inclusão não equivale a deferimento. Regras de renda, composição, frequência escolar, acompanhamento de saúde, residência e revisão cadastral variam conforme programa e podem ser atualizadas.

        ## Consulta segura
        Use o portal ou aplicativo oficial do Cadastro Único, Meu INSS, Caixa Tem ou canal do órgão responsável. Mantenha CPF, endereço, composição familiar e renda corretos. Observe convocações oficiais de atualização e guarde protocolos.

        ## Atenção a golpes
        Não pague para “desbloquear” benefício e não compartilhe senha, código de autenticação ou dados bancários por telefone ou mensagem. Confirme qualquer comunicação diretamente no aplicativo oficial ou no CRAS.
        """),

        new("MEI sem complicação", "mei-sem-complicacao", "Organize a rotina do microempreendedor: formalização, pagamento mensal, declaração e controle do negócio.", "mei", ["mei-portal", "mei-inss", "simple-tax"],
        """
        ## O que é MEI
        O Microempreendedor Individual é uma forma simplificada de formalização para quem exerce atividade permitida e cumpre os requisitos legais. A abertura é feita em canal oficial. O enquadramento depende da atividade, receita, participação em outras empresas e demais condições previstas na legislação vigente.

        ## Exemplo do cotidiano
        Uma pessoa que presta serviços pode separar conta e registros do negócio, emitir documentos fiscais quando exigido, acompanhar receitas mensais e pagar o DAS pelo sistema oficial. Mesmo sem movimento, existem obrigações declaratórias que devem ser conferidas para o ano.

        ## Benefícios e limites
        A formalização facilita CNPJ e acesso a serviços empresariais, mas não elimina obrigações fiscais, municipais ou previdenciárias. A contribuição do MEI pode dar acesso a benefícios do INSS se forem cumpridos os requisitos de cada benefício; carência e qualidade de segurado importam.

        ## Perguntas frequentes
        P: Ser MEI substitui a declaração de Imposto de Renda da pessoa física?
        R: Não necessariamente. As regras de pessoa jurídica e pessoa física são diferentes.

        P: Onde confirmo limite e atividade permitida?
        R: No Portal do Empreendedor e na legislação atual do Simples Nacional.

        ## Conteúdos relacionados
        Imposto de Renda; benefícios do Governo; organização financeira pessoal.
        """,
        """
        ## Obrigações que precisam ser verificadas
        O MEI deve acompanhar pagamento do DAS, declaração anual DASN-SIMEI, registro mensal das receitas, emissão de nota fiscal nos casos exigidos, regras municipais/estaduais e obrigações trabalhistas se contratar empregado. Limites de receita, atividades autorizadas e condições de contratação podem mudar; não use material de anos anteriores sem conferir a fonte vigente.

        ## Previdência
        A contribuição recolhida no DAS está vinculada ao RGPS, mas acesso e cálculo dos benefícios dependem de carência, qualidade de segurado, idade, incapacidade e histórico contributivo. A contribuição simplificada pode não contar para todas as modalidades sem complementação. Consulte INSS e extrato CNIS.

        ## Rotina recomendada
        Separe finanças empresariais e pessoais, emita comprovantes, reserve caixa para tributos, confira calendário oficial e procure contador quando ultrapassar limites ou mudar de enquadramento.
        """),

        new("Cartão de crédito: use com cuidado", "cartao-de-credito-use-com-cuidado", "Entenda fatura, pagamento mínimo, rotativo, parcelamentos e limite antes de usar o cartão como extensão da renda.", "financeiro", ["card-faq", "card-interest-cap", "loan-faq"],
        """
        ## Como funciona a fatura
        O cartão concentra compras para pagamento futuro. Pagar a fatura integral até o vencimento evita financiar o saldo pelo rotativo. Pagar apenas o mínimo não quita a dívida: o restante é uma operação de crédito com juros e encargos.

        ## Exemplo do cotidiano
        Antes de parcelar uma compra, some parcelas já contratadas e contas essenciais dos próximos meses. Confira se o parcelamento é sem juros ou se tem custo e compare com o preço à vista.

        ## Limite e riscos
        O limite disponível não é renda. A regra brasileira limita juros e encargos financeiros do rotativo e do parcelamento do saldo da fatura, para dívidas originadas desde 3 de janeiro de 2024, a 100% do valor original da dívida. Isso não torna a modalidade barata: o saldo pode dobrar e a regra não abrange compra parcelada com juros da mesma forma.

        ## Dicas
        Ative alertas, confira lançamentos, pague em canal seguro, bloqueie cartão perdido e conteste compras não reconhecidas imediatamente.

        ## Perguntas frequentes
        P: O pagamento mínimo evita juros?
        R: Não. O saldo não pago é financiado e pode gerar juros e encargos.

        P: O limite do cartão melhora minha renda disponível?
        R: Não. Cada compra cria compromisso futuro.

        ## Conteúdos relacionados
        Empréstimos; juros; score de crédito.
        """,
        """
        ## Rotativo e parcelamento de fatura
        O crédito rotativo é utilizado quando a fatura não é paga integralmente e o saldo permanece financiado. As regras limitam o total de juros e encargos financeiros sobre saldo devedor originado a partir de 3 de janeiro de 2024 a 100% do valor original financiado. A regra alcança o saldo migrado para parcelamento da fatura, mas não transforma compras parceladas com juros em operação idêntica.

        ## Leitura da fatura
        Confira valor total, vencimento, pagamento mínimo, encargos, parcelas futuras e transações. Compare eventual parcelamento oferecido com outras linhas de crédito pelo CET e total a pagar. O pagamento mínimo serve para evitar inadimplência imediata segundo contrato, não para reduzir custo.

        ## Segurança
        Não informe número completo, CVV, senha ou token por telefone/mensagem. Para contestar compra, use o canal oficial do emissor e guarde protocolo.
        """),

        new("Score de crédito", "score-de-credito", "Saiba o que a pontuação de crédito representa, por que varia entre gestores e como cuidar do seu histórico.", "financeiro", ["positive-registry", "registrato", "loan-faq"],
        """
        ## O que é score
        Score é uma pontuação calculada por gestores de dados para apoiar análises de risco. Não existe uma nota única do Banco Central que garanta aprovação. Cada gestor pode usar metodologia própria e cada credor avalia também renda, modalidade, garantias e política interna.

        ## Exemplo do cotidiano
        Uma conta paga em dia pode contribuir para o histórico positivo, mas não assegura aumento de score nem concessão de crédito. Informações desatualizadas ou incorretas devem ser contestadas diretamente com o gestor responsável.

        ## Como cuidar do histórico
        Pague compromissos no prazo, evite assumir parcelas incompatíveis com o orçamento, confira seu Cadastro Positivo e consulte o Registrato para identificar operações financeiras que não reconhece. Não pague empresas que prometem “aumentar a pontuação” rapidamente.

        ## Perguntas frequentes
        P: Score alto obriga o banco a conceder crédito?
        R: Não. A decisão de crédito é da instituição e considera diversos critérios.

        P: O Registrato mostra meu score?
        R: O Registrato reúne relatórios de relacionamentos, operações e outros dados do sistema financeiro; não é uma nota de crédito universal.

        ## Conteúdos relacionados
        Cartão de crédito; empréstimos; golpes financeiros e segurança digital.
        """,
        """
        ## Cadastro Positivo e gestores
        O Cadastro Positivo registra histórico de pagamentos e pode incluir pontuações calculadas por gestores de bancos de dados. As metodologias não são necessariamente iguais. O consumidor pode acessar informações, pedir correção e exercer direitos previstos na legislação perante o gestor responsável.

        ## Registrato
        O relatório do Banco Central permite consultar informações que instituições financeiras compartilham, como relacionamentos e operações. O acesso exige autenticação gov.br conforme as regras do serviço. Diferenças de atualização entre sistemas podem existir; conteste a origem do dado com a instituição responsável.

        ## Limite da pontuação
        Score não é avaliação completa de capacidade financeira e não garante aprovação, taxa ou limite. Desconfie de venda de “limpeza” de histórico ou alteração garantida de pontuação.
        """),

        new("Organização financeira pessoal", "organizacao-financeira-pessoal", "Monte um orçamento simples para entender entradas, despesas, prioridades e compromissos futuros.", "financeiro", ["budget-guide", "emergency-fund"],
        """
        ## Comece pelo registro
        Anote renda líquida e despesas durante um período. Agrupe gastos essenciais, compromissos, variáveis e ocasionais. A meta não é eliminar todo lazer, mas entender para onde o dinheiro vai e decidir prioridades com antecedência.

        ## Exemplo do cotidiano
        Se o salário entra no início do mês e uma conta anual vence em dezembro, reserve uma parte mensal para essa despesa. Assim, o orçamento considera compromissos futuros em vez de tratar o saldo de hoje como dinheiro livre.

        ## Método prático
        Planeje, registre, agrupe e avalie. Compare planejado com realizado, revise assinaturas e tarifas e defina uma meta possível de poupança. Se a conta não fecha, ajuste primeiro despesas negociáveis e busque apoio para reorganizar dívidas.

        ## Perguntas frequentes
        P: Preciso de aplicativo?
        R: Não. Papel, planilha ou ferramenta digital funcionam se forem usados regularmente.

        P: O que faço se as despesas superam a renda?
        R: Mapeie vencimentos e juros, priorize necessidades básicas e negocie com credores antes de assumir novo crédito.

        ## Conteúdos relacionados
        Reserva de emergência; planejamento financeiro familiar; endividamento.
        """,
        """
        ## Ciclo de orçamento
        Um orçamento útil combina planejamento de receitas e despesas futuras com registro e avaliação dos valores realizados. Inclua gastos previsíveis não mensais — impostos, manutenção, matrícula e saúde — distribuídos ao longo do ano. Diferencie renda certa de variável e evite comprometer receitas ainda incertas.

        ## Indicadores pessoais
        Acompanhe saldo mensal, parcelas futuras, despesas essenciais, custo das dívidas e valor destinado a objetivos. Não há proporção universal que sirva para todas as famílias; adapte o plano a renda, dependentes, localidade e estabilidade do trabalho.

        ## Ajuste
        Se houver déficit persistente, negocie serviços, reveja gastos recorrentes e busque orientação de Procon ou serviço público quando necessário. Evite usar crédito caro para encobrir continuamente a diferença entre renda e despesa.
        """),

        new("Reserva de emergência", "reserva-de-emergencia", "Planeje um valor de acesso rápido para imprevistos sem confundir reserva com dinheiro de objetivos ou investimentos de longo prazo.", "financeiro", ["emergency-fund", "budget-guide", "investment-taxes"],
        """
        ## Para que serve
        Reserva de emergência é dinheiro separado para eventos inesperados, como perda de renda, reparo urgente ou despesa médica. Ela ajuda a evitar recorrer imediatamente a crédito caro. O valor adequado depende das despesas essenciais, estabilidade de renda, dependentes e proteções disponíveis.

        ## Exemplo do cotidiano
        Se uma pessoa tem despesas essenciais mensais e renda instável, pode definir uma meta maior do que alguém com renda previsível e outra rede de proteção. Comece com aportes regulares que caibam no orçamento e revise a meta quando a vida mudar.

        ## Onde guardar
        Priorize segurança, liquidez e simplicidade. Entenda risco de crédito, regras de resgate, oscilação e tributação do produto. Retorno alto não deve ser o critério principal para dinheiro que pode ser necessário rapidamente.

        ## Perguntas frequentes
        P: Quantos meses de despesas devo guardar?
        R: Não há um número único para todas as pessoas; estime conforme gastos e estabilidade da renda.

        P: Posso usar a reserva para uma promoção?
        R: Planeje compras e objetivos em outra poupança; preserve a reserva para imprevistos.

        ## Conteúdos relacionados
        Organização financeira pessoal; IOF e rendimentos; planejamento familiar.
        """,
        """
        ## Dimensionamento
        O Banco Central recomenda que a reserva corresponda a alguns meses dos gastos usuais, mas o horizonte é individual. Considere despesas essenciais, número de dependentes, previsibilidade de renda, acesso a seguro ou benefício e tempo esperado para recompor renda.

        ## Liquidez e risco
        Mantenha o recurso em alternativa de baixo risco e resgate compatível com a urgência. Compare cobertura de garantias, risco de emissor, prazo de liquidação, custos e tributação. Produto com carência ou alta volatilidade pode não cumprir a função da reserva.

        ## Uso e reposição
        Defina o que caracteriza emergência, registre retiradas e retome aportes quando a situação estabilizar. Se a reserva for insuficiente, reavalie despesas e fontes de renda; evite promessas de rendimento garantido fora de instituições autorizadas.
        """),

        new("FGTS e INSS", "fgts-e-inss", "Entenda a diferença entre o saldo do FGTS e a proteção previdenciária do INSS e onde consultar cada informação.", "trabalhista", ["fgts-withdrawal", "inss-contributions", "mei-inss"],
        """
        ## São sistemas diferentes
        O FGTS é uma conta vinculada com depósitos do empregador para trabalhadores abrangidos pela legislação. O saque depende de hipóteses previstas em lei e não é livre a qualquer momento. O INSS administra benefícios previdenciários e exige contribuição e cumprimento dos requisitos de cada benefício.

        ## Exemplo do cotidiano
        Ao mudar de emprego, consulte o extrato do FGTS para conferir depósitos. Para planejar aposentadoria ou benefício, verifique vínculos e contribuições no CNIS pelo Meu INSS; divergências devem ser corrigidas com documentos.

        ## Pontos de atenção
        Ter saldo no FGTS não substitui contribuição ao INSS. Contribuições em atraso, períodos sem registro ou pagamento como MEI podem ter regras próprias. Não tome decisão de saque ou aposentadoria com base apenas em simulador não oficial.

        ## Perguntas frequentes
        P: Posso sacar FGTS quando quiser?
        R: Não. O saque depende das hipóteses legais e da modalidade escolhida.

        P: Pagar DAS como MEI garante qualquer aposentadoria?
        R: Não automaticamente. Carência, idade, qualidade de segurado e regras da contribuição precisam ser verificadas.

        ## Conteúdos relacionados
        Benefícios do Governo; MEI sem complicação; organização financeira.
        """,
        """
        ## Fundo de Garantia
        O FGTS recebe depósitos vinculados ao contrato de trabalho abrangido pela Lei nº 8.036/1990. A movimentação da conta depende das hipóteses legais, incluindo situações de desligamento, moradia, aposentadoria e outras previstas. Modalidades de saque e efeitos sobre operações de crédito devem ser conferidos no canal oficial antes de optar.

        ## Previdência Social
        O INSS considera vínculos, remunerações e contribuições registrados no CNIS. Benefícios têm requisitos diferentes de carência, idade, incapacidade, qualidade de segurado e cálculo. Contribuinte individual e MEI possuem formas e códigos próprios de recolhimento; complementação pode ser necessária para determinados fins.

        ## Conferência
        Use aplicativo e portal FGTS/Caixa para o fundo e Meu INSS para extrato previdenciário. Guarde carteira de trabalho, guias, recibos e documentos de atividade.
        """),

        new("Golpes financeiros e segurança digital", "golpes-financeiros-seguranca-digital", "Reconheça sinais de fraude, proteja contas e saiba quais medidas tomar rapidamente se ocorrer uma transação suspeita.", "financeiro", ["fraud-faq", "pix-security", "positive-registry"],
        """
        ## Sinais de alerta
        Golpistas criam urgência, prometem retorno garantido, fingem ser banco ou familiar e pedem transferência, senha, código ou instalação de aplicativo. Confirme pedidos por outro canal e verifique nome e instituição do destinatário antes de pagar.

        ## Exemplo do cotidiano
        Uma mensagem diz que sua conta será bloqueada e pede um código recebido por SMS. Não responda nem clique. Abra o aplicativo bancário digitando o endereço oficial ou ligue para o número do cartão.

        ## Se você foi vítima
        Contate imediatamente seu banco pelo canal oficial, relate horário, valor e dados da transação e solicite providências. Em golpe via Pix, pergunte sobre contestação e Mecanismo Especial de Devolução; a análise não garante recuperação. Preserve conversas e comprovantes, registre boletim de ocorrência e procure Procon se necessário.

        ## Perguntas frequentes
        P: O banco pode pedir minha senha completa por mensagem?
        R: Não informe senha ou código de autenticação a terceiros.

        P: O MED garante que o Pix será devolvido?
        R: Não. É um mecanismo de análise e possível devolução conforme regras e recursos disponíveis.

        ## Conteúdos relacionados
        Score de crédito; cartão de crédito; endividamento e renegociação.
        """,
        """
        ## Prevenção
        Use autenticação em dois fatores, bloqueio de tela e alertas de movimentação. Atualize sistema e aplicativos apenas por lojas oficiais. Confirme beneficiário, instituição, valor e finalidade antes de autorizar Pix, boleto ou transferência. Bancos não solicitam token para “cancelar” fraude por ligação recebida.

        ## Resposta a incidente
        Avise imediatamente a instituição pagadora e, se identificável, a instituição recebedora. Para Pix, registre contestação no canal do prestador e solicite avaliação pelo MED. Faça boletim de ocorrência, proteja e-mail e contas vinculadas, troque senhas comprometidas por dispositivo confiável e monitore novas transações.

        ## Limite de recuperação
        Bloqueio ou devolução depende de análise e disponibilidade dos recursos, e não é garantido. Guarde protocolos e escale para ouvidoria, Procon ou reclamação ao Banco Central conforme o caso.
        """),

        new("Endividamento e renegociação de dívidas", "endividamento-e-renegociacao-de-dividas", "Organize o que deve, avalie propostas sustentáveis e conheça canais de defesa do consumidor.", "financeiro", ["overindebtedness", "consumer-law", "portability-faq"],
        """
        ## Primeiro, entenda o quadro
        Liste credor, saldo atualizado, parcela, juros, atraso, garantia e consequências do não pagamento. Separe despesas essenciais e renda líquida antes de assumir uma proposta. Evite negociar sem saber o saldo e o total do novo acordo.

        ## Exemplo do cotidiano
        Se uma proposta reduz a parcela, mas estende muito o prazo, calcule o valor total e pergunte sobre descontos, encargos e consequências de atraso. Um acordo só ajuda se couber no orçamento até o fim.

        ## Direitos e apoio
        Procure primeiro o credor em canal oficial. Se não houver solução, consulte Procon, Consumidor.gov.br ou orientação jurídica. A Lei do Superendividamento prevê mecanismos para tratamento da situação de pessoa consumidora de boa-fé, observadas as condições e exclusões legais.

        ## Perguntas frequentes
        P: Devo pagar um intermediário para limpar meu nome?
        R: Não. Confirme a negociação com o credor e use plataformas oficiais.

        P: Todo tipo de dívida entra no procedimento de superendividamento?
        R: Não. A lei tem âmbito, condições e exclusões; procure o órgão de defesa do consumidor.

        ## Conteúdos relacionados
        Portabilidade de dívidas; empréstimos; organização financeira pessoal.
        """,
        """
        ## Diagnóstico e proposta
        Consolide contratos, extratos e demonstrativos. Verifique taxa, CET, saldo para liquidação e encargos de atraso. Priorize riscos que afetem moradia, serviços essenciais, trabalho ou garantias, sem ignorar prazos de defesa. Proponha valor, entrada e calendário compatíveis com orçamento realista.

        ## Superendividamento
        O Código de Defesa do Consumidor, alterado pela Lei nº 14.181/2021, prevê prevenção e tratamento do superendividamento da pessoa natural de boa-fé, considerando impossibilidade manifesta de pagar dívidas de consumo sem comprometer o mínimo existencial. Existem exclusões legais e procedimentos específicos; o Procon pode orientar.

        ## Segurança na renegociação
        Confirme beneficiário do boleto, credor, valor de quitação, condições de baixa e canal oficial. Não compartilhe senha gov.br ou bancária. Guarde proposta, contrato, comprovantes e protocolos.
        """),

        new("Juros: entenda antes de comprar ou contratar", "juros-entenda-antes-de-comprar", "Aprenda como juros, prazo e frequência de capitalização influenciam o custo de compras, empréstimos e aplicações.", "financeiro", ["loan-faq", "budget-guide", "investment-taxes"],
        """
        ## O que são juros
        Juros remuneram o uso do dinheiro ao longo do tempo. Em uma dívida, são custo para quem toma crédito; em uma aplicação, podem compor o retorno, que também depende de risco, prazo, taxas e impostos.

        ## Simples e compostos
        Nos juros simples, a taxa incide sobre o principal em cada período. Nos compostos, os juros acumulados passam a integrar a base dos períodos seguintes. Por isso, a taxa e o prazo precisam ser observados juntos.

        ## Exemplo do cotidiano
        Compare preço à vista e soma de parcelas, incluindo entrada. Em crédito, compare CET e total a pagar. Em uma aplicação, verifique retorno líquido e se o resgate ocorre antes do prazo previsto.

        ## Perguntas frequentes
        P: Posso multiplicar a taxa mensal por 12 para obter a taxa anual efetiva?
        R: Não em juros compostos. A equivalência considera capitalização; a taxa efetiva anual é calculada por (1 + taxa mensal)^12 − 1.

        P: Taxa menor sempre significa menor custo?
        R: Não, se prazo, encargos e valor financiado forem diferentes.

        ## Conteúdos relacionados
        Empréstimos; IOF e rendimentos; consórcio ou financiamento.
        """,
        """
        ## Convenção de taxa
        Identifique período, regime de capitalização e se a taxa é nominal ou efetiva. Para uma taxa efetiva mensal i, a taxa efetiva anual equivalente é (1+i)^12−1. Não some períodos compostos como se juros fossem lineares. Em pagamentos, calendário, carência, amortização e arredondamentos também mudam o resultado.

        ## Custo de crédito
        A taxa de juros é apenas um componente; CET reúne custos da operação de crédito. Para comparar, use fluxos equivalentes, mesmo valor líquido e prazo. Em compra parcelada, some entrada e prestações e confirme se há desconto à vista.

        ## Retorno de aplicação
        Retorno esperado não é retorno garantido. Considere risco de mercado e emissor, liquidez, tributação, taxas e inflação. Simuladores ilustram hipóteses e não predizem resultados futuros.
        """),

        new("Planejamento financeiro familiar", "planejamento-financeiro-familiar", "Conversem sobre renda, despesas, objetivos e imprevistos para construir um plano financeiro que faça sentido para a família.", "financeiro", ["budget-guide", "emergency-fund"],
        """
        ## Planejem em conjunto
        Um orçamento familiar registra rendas e despesas de quem participa das decisões. Definam objetivos comuns — contas, moradia, estudos, lazer e reserva — e respeitem também prioridades individuais acordadas.

        ## Exemplo do cotidiano
        Uma família pode dividir despesas por vencimento e reservar mensalmente para material escolar ou manutenção da casa. Conversar antes sobre compras maiores evita comprometer o dinheiro destinado a contas essenciais.

        ## Como começar
        Registrem entradas, gastos e dívidas; identifiquem despesas anuais; definam metas curtas e longas; escolham quem acompanha os pagamentos; revisem o plano mensalmente. Se a renda for variável, usem uma previsão conservadora e ajustem quando receberem.

        ## Perguntas frequentes
        P: Todos precisam ter a mesma renda para dividir as contas?
        R: Não há fórmula universal. A família pode combinar uma divisão transparente e sustentável para sua realidade.

        P: E quando surge um gasto inesperado?
        R: Usem a reserva se for emergência e revisem o orçamento para recompô-la.

        ## Conteúdos relacionados
        Organização financeira pessoal; reserva de emergência; educação financeira para jovens.
        """,
        """
        ## Método compartilhado
        Planejamento familiar requer dados consensuais e atualizados. Registre rendas líquidas, despesas fixas e variáveis, dívidas, obrigações sazonais e objetivos. Defina responsabilidades, datas de revisão e limites para decisões individuais ou conjuntas. Uma pessoa pode manter a planilha, mas as decisões relevantes devem ser compreendidas por quem será afetado.

        ## Metas e proteção
        Transforme objetivos em prazo, valor e contribuição planejada. Priorize despesas essenciais, proteção contra imprevistos e dívidas de custo alto antes de assumir novos compromissos. Evite expor dados de crianças ou senhas financeiras em documentos compartilhados.

        ## Ajustes
        Reavalie o plano quando houver nascimento, mudança de trabalho, separação, doença, mudança de cidade ou alteração relevante na renda. O orçamento é instrumento de decisão, não regra rígida.
        """),

        new("Educação financeira para jovens", "educacao-financeira-para-jovens", "Aprenda a planejar objetivos, poupar, comparar escolhas e proteger dinheiro e dados no ambiente digital.", "financeiro", ["youth-education", "youth-program", "budget-guide"],
        """
        ## Comece com objetivos
        Educação financeira ajuda a decidir como usar recursos hoje e no futuro. Escolha um objetivo concreto, estime quanto custa e quanto tempo levará para juntar. Registre entradas e gastos para perceber como pequenas decisões se acumulam.

        ## Exemplo do cotidiano
        Se recebe mesada ou renda de estágio, separe uma parte para transporte e outras necessidades, outra para um objetivo e reserve uma margem para imprevistos. Compare preço total, prazo e juros antes de comprar parcelado.

        ## Crédito e segurança
        Cartão e crédito não são renda extra: são compromissos futuros. Não empreste conta ou chave Pix para movimentar dinheiro de desconhecidos. Proteja senhas, confirme compras e desconfie de promessas de dinheiro fácil.

        ## Perguntas frequentes
        P: Preciso ganhar muito para começar a poupar?
        R: Não. O principal é conhecer a renda e escolher uma meta compatível; pequenos valores ajudam a criar hábito.

        P: Onde estudar mais?
        R: O Banco Central mantém o Aprender Valor e materiais gratuitos de educação financeira.

        ## Conteúdos relacionados
        Organização financeira pessoal; cartão de crédito; golpes financeiros e segurança digital.
        """,
        """
        ## Habilidades práticas
        Desenvolva planejamento, poupança ativa, comparação de preços e compreensão do crédito. Diferencie necessidade, desejo e publicidade; calcule custo total de compras parceladas; leia regras de serviços digitais; reconheça juros e riscos básicos. Use situações reais com apoio de adulto responsável quando envolver contrato ou conta bancária.

        ## Proteção digital
        Não compartilhe códigos de autenticação nem permita que terceiros usem sua conta para receber ou encaminhar valores. Golpes de emprego, investimento e compra online frequentemente exploram urgência e promessa de ganho. Confirme identidade e canal antes de transferir.

        ## Material educativo
        O Aprender Valor é iniciativa gratuita do Banco Central voltada à educação financeira nas escolas. Seus eixos incluem planejar recursos, poupar e usar crédito com responsabilidade; professores podem acessar projetos e orientações oficiais.
        """)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var categories = await db.Categories.ToDictionaryAsync(x => x.Slug, cancellationToken);
        var missingCategories = Articles.Select(x => x.CategorySlug).Distinct().Where(x => !categories.ContainsKey(x)).ToArray();
        if (missingCategories.Length > 0)
            throw new InvalidOperationException($"Missing education article categories: {string.Join(", ", missingCategories)}.");

        var sourceByUrl = await db.Sources.ToDictionaryAsync(x => x.Url, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var definition in Sources)
        {
            if (sourceByUrl.ContainsKey(definition.Url)) continue;
            var source = new Source
            {
                Name = definition.Name,
                Url = definition.Url,
                SourceType = definition.Type,
                Publisher = definition.Publisher,
                IsOfficial = true,
                IsActive = true,
                RetrievedAt = now,
                LastVerifiedAt = now
            };
            db.Sources.Add(source);
            sourceByUrl.Add(source.Url, source);
        }

        var existingSlugs = await db.Articles.Select(x => x.Slug).ToHashSetAsync(cancellationToken);
        var added = 0;
        foreach (var definition in Articles.Where(x => !existingSlugs.Contains(x.Slug)))
        {
            var article = new Article
            {
                Title = definition.Title,
                Slug = definition.Slug,
                Summary = definition.Summary,
                SimpleContent = definition.SimpleContent.Trim(),
                TechnicalContent = definition.TechnicalContent.Trim(),
                CategoryId = categories[definition.CategorySlug].Id,
                Status = ArticleStatus.Published,
                ReviewStatus = ReviewStatus.Reviewed,
                PublishedAt = now,
                LastReviewedAt = now,
                NeedsReviewAt = now.AddMonths(6),
                ValidFrom = DateOnly.FromDateTime(now.UtcDateTime),
                ContentVersion = 1
            };
            foreach (var sourceKey in definition.SourceKeys)
            {
                var source = Sources.FirstOrDefault(x => x.Key == sourceKey)
                    ?? throw new InvalidOperationException($"Article '{definition.Slug}' references unknown source key '{sourceKey}'.");
                var persisted = sourceByUrl[source.Url];
                article.ArticleSources.Add(new ArticleSource { Article = article, Source = persisted });
            }
            db.Articles.Add(article);
            existingSlugs.Add(definition.Slug);
            added++;
        }

        var sampleArticle = await db.Articles.Include(x => x.ArticleSources).FirstOrDefaultAsync(x => x.Slug == "entenda-juros-compostos", cancellationToken);
        if (sampleArticle is not null && sampleArticle.ReviewStatus != ReviewStatus.Reviewed)
        {
            sampleArticle.Title = "Juros compostos: como o tempo afeta o saldo";
            sampleArticle.Summary = "Entenda como os juros de cada período se incorporam ao saldo e por que prazo, taxa e momento dos aportes alteram uma simulação.";
            sampleArticle.SimpleContent = """
                ## Como funcionam
                No regime composto, os juros de cada período são incorporados ao saldo. No período seguinte, o cálculo usa o saldo atualizado. Para um capital sem novos aportes, M = C × (1 + i)^n, em que C é o capital inicial, i a taxa por período e n o número de períodos.

                ## Exemplo educativo
                Com R$ 1.000, taxa constante de 1% ao mês e 12 meses, a fórmula resulta em aproximadamente R$ 1.126,83 antes de impostos e custos. É uma conta hipotética: taxas de investimentos e crédito não são garantidas e podem ter custos e regras próprios.

                ## Aportes regulares
                Aportes ao início do mês rendem por mais um período do que depósitos ao fim. Confira qual convenção o simulador utiliza e se a taxa informada tem a mesma periodicidade do prazo.

                P: Juros compostos garantem retorno positivo em um investimento?
                R: Não. A fórmula apenas projeta os valores para uma taxa informada; não prevê desempenho nem risco de mercado.

                ## Ferramenta
                Use a calculadora educativa de juros compostos e confira os parâmetros antes de interpretar o resultado.
                """;
            sampleArticle.TechnicalContent = """
                ## Fórmula e unidades
                Sem aportes, FV = PV × (1 + i)^n. A taxa i e o período n precisam ter periodicidade compatível. A taxa anual efetiva equivalente a uma taxa mensal constante é (1 + i_m)^12 − 1.

                ## Aportes
                Para pagamentos ao fim dos períodos, FV = PV × (1+i)^n + PMT × (((1+i)^n − 1) / i). Aportes no início multiplicam o fator de aportes por (1+i). Para taxa zero, o fator limite é n.

                ## Limites
                A conta assume taxa constante, capitalização regular e os fluxos definidos. Ela não considera automaticamente impostos, taxas, liquidez, risco, inflação ou variações de preço. O resultado da Calculadora do Cidadão também depende dos parâmetros informados e pode diferir de contratos.
                """;
            sampleArticle.Status = ArticleStatus.Published;
            sampleArticle.ReviewStatus = ReviewStatus.Reviewed;
            sampleArticle.LastReviewedAt = now;
            sampleArticle.NeedsReviewAt = now.AddMonths(6);
            sampleArticle.ContentVersion++;
            foreach (var url in new[] { "https://www.bcb.gov.br/meubc/calculadoradocidadao", "https://www.bcb.gov.br/content/config/aprendervalor/docs_aprender_valor/OLITEF_2024_CADERNO_ESTUDANTE_VFinal.pdf" })
            {
                var source = sourceByUrl[url];
                source.LastVerifiedAt = now;
                if (!sampleArticle.ArticleSources.Any(x => x.SourceId == source.Id || x.Source?.Url == url)) sampleArticle.ArticleSources.Add(new ArticleSource { Article = sampleArticle, Source = source });
            }
        }        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Financial education content provisioned: {Articles} new articles; {Sources} official sources.", added, Sources.Length);
    }

    private sealed record SourceDefinition(string Key, string Name, string Url, string Publisher, SourceType Type)
    {
        public SourceDefinition(string name, string url, string publisher, SourceType type)
            : this(KeyFromName(name), name, url, publisher, type) { }

        private static string KeyFromName(string name) => name switch
        {
            "Tributação de altas rendas — Receita Federal" => "high-income",
            "Perguntas e respostas sobre tributação de altas rendas — Receita Federal" => "high-income-qa",
            "IRPF — Perguntas e respostas" => "irpf-faq",
            "Tributação de 2026" => "irpf-2026",
            "Portabilidade de crédito — como solicitar" => "portability-faq",
            "Saiba como fazer a portabilidade de crédito" => "portability-guide",
            "Administradoras de consórcio" => "consortium",
            "Perguntas frequentes sobre consórcio" => "consortium-faq",
            "Empréstimos e financiamentos — perguntas frequentes" => "loan-faq",
            "Tipos de empréstimos disponíveis" => "loan-types",
            "Caderno de educação financeira — gestão das finanças pessoais" => "budget-guide",
            "Lidando com imprevistos e reserva de emergência" => "emergency-fund",
            "IOF em investimentos — Portal do Investidor" => "investment-taxes",
            "Compra de títulos públicos — tributação" => "treasury-tax",
            "Constituição Federal — competências tributárias" => "constitution-taxes",
            "Meu Imposto de Renda" => "irpf-home",
            "Cadastro Único" => "cadunico",
            "Bolsa Família — receber o benefício" => "bolsa-family",
            "Benefício de Prestação Continuada — BPC" => "benefits-bpc",
            "Portal do Empreendedor" => "mei-portal",
            "Perguntas e respostas sobre o MEI — Simples Nacional" => "simple-tax",
            "Microempreendedor Individual — direitos previdenciários" => "mei-inss",
            "Cartão de crédito — perguntas frequentes" => "card-faq",
            "Limite de juros e encargos no cartão" => "card-interest-cap",
            "Cadastro Positivo — perguntas frequentes" => "positive-registry",
            "Registrato" => "registrato",
            "FGTS — saques" => "fgts-withdrawal",
            "Contribuição previdenciária e salário de contribuição" => "inss-contributions",
            "Golpes — como evitar e o que fazer" => "fraud-faq",
            "Segurança no Pix" => "pix-security",
            "Superendividamento" => "overindebtedness",
            "Consumidor.gov.br — legislação de proteção do consumidor" => "consumer-law",
            "Aprender Valor — educação financeira para crianças e jovens" => "youth-education",
            "Aprender Valor — perguntas frequentes" => "youth-program",
            "Taxas de juros médias por modalidade" => "interest-basics",
            "IPVA — orientações da Secretaria da Fazenda de Pernambuco" => "ipva-pe",
            _ => throw new InvalidOperationException($"No source key for {name}.")
        };
    }

    private sealed record ArticleDefinition(string Title, string Slug, string Summary, string CategorySlug, string[] SourceKeys, string SimpleContent, string TechnicalContent);
}
