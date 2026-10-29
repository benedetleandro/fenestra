(function () {
  "use strict";

  var PT = {
    "nav.how": "Como funciona",
    "nav.compat": "Compatibilidade",
    "nav.install": "Instalar",
    "nav.faq": "Dúvidas",

    "hero.badge": "Windows 10/11 · Edge, Chrome, Firefox · Sem dependências",
    "hero.title": "Dois cliques.<br><span class=\"shine\">E roda.</span>",
    "hero.lead": "O Fenestra é um servidor local minúsculo que transforma qualquer <code>dist/index.html</code> num site funcionando no seu navegador. Sem terminal, sem extensão, sem navegador próprio.",
    "hero.download": "Baixar v0.2.0",
    "hero.how": "Como funciona",
    "hero.meta1": "Licença MIT",
    "hero.meta2": "sem SDK .NET",
    "hero.meta3": "um arquivo, sem instalador",

    "problem.kicker": "O problema",
    "problem.title": "O problema é a origem, não o arquivo",
    "problem.lead": "Builds modernos usam módulos ES. Em <code>file://</code> o navegador trata a página como origem opaca: módulos são bloqueados por CORS, <code>/assets/...</code> aponta para a raiz do disco e service worker não existe. O arquivo está certo. A origem não.",
    "problem.badLead": "O que acontece quando você dá dois cliques num build hoje:",
    "problem.bad1": "Módulos bloqueados por CORS",
    "problem.bad2": "Caminhos absolutos apontam para a raiz do disco",
    "problem.bad3": "Sem service worker, sem manifest, tela branca",
    "problem.goodLead": "O que acontece quando o Fenestra serve a mesma pasta:",
    "problem.good1": "Módulos carregam, igual em produção",
    "problem.good2": "Rotas de SPA sobrevivem ao F5",
    "problem.good3": "Mesma aba, mesmo perfil, seu navegador",

    "how.kicker": "Como funciona",
    "how.title": "Três passos, nenhum deles seu",
    "how.lead": "O Fenestra se acopla ao duplo clique e decide o que o arquivo precisa.",
    "how.s1t": "Você abre o arquivo",
    "how.s1d": "Dois cliques, arrastar a pasta sobre o executável, ou um caminho na linha de comando.",
    "how.s2t": "O Fenestra decide",
    "how.s2d": "Ele lê o HTML. Precisa de origem HTTP? Sobe um servidor numa porta estável. Não precisa? Abre o arquivo direto.",
    "how.s3t": "O seu navegador mostra",
    "how.s3d": "Uma aba normal, na sua janela, com seu perfil, suas extensões e sua barra de endereço.",

    "compat.kicker": "Compatibilidade",
    "compat.title": "O que funciona, o que não funciona",
    "compat.lead": "Se a pasta funciona em qualquer host estático, funciona aqui. Se precisa de build ou de backend, não funciona — e o Fenestra avisa, em vez de falhar em silêncio.",
    "compat.h1": "Caso",
    "compat.h2": "Funciona?",
    "compat.h3": "Por quê",
    "compat.yes": "Sim",
    "compat.no": "Não",
    "compat.c1": "HTML, CSS e JS puros servidos da pasta.",
    "compat.c2": "Rotas sem extensão caem no index.html, então o F5 funciona.",
    "compat.c3": "Aberto direto pelo Edge via file://, sem servidor nenhum.",
    "compat.c4": "Projeto fonte precisa do dev server dele para compilar.",
    "compat.c5": "O Fenestra serve arquivos. Não faz proxy, não roda serviços, não toca em banco.",
    "compat.c6": "A raiz é a pasta do arquivo, e o .. é bloqueado por segurança.",

    "install.kicker": "Instalação",
    "install.title": "Um executável. Sem instalador.",
    "install.lead": "Windows 10 ou 11 e qualquer navegador moderno. É a lista de requisitos inteira — o .NET Framework usado para compilar já vem no Windows.",
    "install.dl": "Baixar",
    "install.dlText": "Pegue o Fenestra.exe na release mais recente e coloque onde quiser.",
    "install.dlBtn": "Baixar v0.2.0",
    "install.signNote": "O binário ainda não tem assinatura de código, então o SmartScreen do Windows vai pedir confirmação uma vez. É esperado numa versão alfa.",
    "install.build": "Ou compile você mesmo",
    "install.defTitle": "Tornar padrão para .html",
    "install.defText": "Uma vez, três cliques. O Windows não deixa um programa fazer isso sozinho.",
    "install.def1": "Botão direito em qualquer index.html e escolha <b>Abrir com → Escolher outro aplicativo</b>.",
    "install.def2": "Selecione <b>Fenestra</b>.",
    "install.def3": "Marque <b>Sempre usar este aplicativo</b> e confirme.",
    "install.note": "O Windows permite exatamente um manipulador por extensão. O manipulador é invisível; a janela que abre é o Edge.",
    "install.usage": "Uso",

    "under.kicker": "Por dentro",
    "under.title": "Pequeno de propósito",
    "under.lead": "Um arquivo C#, sem dependências, compilado pelo compilador que já vem no Windows. Menos de mil linhas — pequeno o suficiente para auditar numa tarde.",
    "under.p1b": "Servidor",
    "under.p1s": "HTTP/1.1 num TcpListener, ligado só a 127.0.0.1. Nunca 0.0.0.0.",
    "under.p2b": "Caminhos",
    "under.p2s": "Normalizados e confinados à raiz servida. .., : e ::$DATA são rejeitados.",
    "under.p3b": "Portas",
    "under.p3s": "Hash FNV-1a da pasta mapeia para uma porta guardada e reaproveitada, então o localStorage sobrevive.",
    "under.p4b": "Fallback de SPA",
    "under.p4s": "Requisições sem extensão devolvem index.html; asset inexistente continua 404.",
    "under.p5b": "Instância única",
    "under.p5s": "Um segundo duplo clique encontra o servidor ativo e só abre outra aba.",
    "under.p6b": "Ciclo de vida",
    "under.p6s": "Encerra quando o Edge que ele abriu fecha, ou após quatro horas ociosas.",

    "sec.kicker": "Segurança",
    "sec.title": "Local significa local",
    "sec.lead": "Só loopback, sem telemetria, sem conta, sem código remoto. Serve a pasta que você abriu e nada além — mas todo arquivo sob aquela pasta é alcançável, inclusive dotfiles.",
    "sec.note": "Não aponte o Fenestra para um diretório com segredos. O modelo de ameaça completo está no SECURITY.md do repositório.",

    "faq.kicker": "Dúvidas",
    "faq.title": "Perguntas que as pessoas realmente fazem",
    "faq.q1": "Funciona com qualquer site estático?",
    "faq.a1": "Sim. Qualquer pasta com um index.html é servida como está. Cada pasta ganha uma porta estável própria, então dois apps podem ficar abertos ao mesmo tempo sem brigar.",
    "faq.q2": "Preciso de Node.js ou de algum runtime?",
    "faq.a2": "Não. O Fenestra é um executável de 47 KB compilado contra o .NET Framework que já vem no Windows. Nada para instalar, nada para manter atualizado.",
    "faq.q3": "Por que o Windows pede confirmação para o .html?",
    "faq.a3": "Porque o Windows permite exatamente um programa por extensão e não deixa um app reivindicar isso em silêncio. É proteção contra justamente o tipo de software que sequestra associações de arquivo.",
    "faq.q4": "Ele altera meus arquivos?",
    "faq.a4": "Não. Ele abre em modo leitura, serve e não escreve nada no projeto. O único estado que guarda é um número de porta por pasta, em %LOCALAPPDATA%.",
    "faq.q5": "Quais navegadores ele suporta?",
    "faq.a5": "Edge é o padrão. Chrome, Firefox, Brave, Vivaldi, Opera e Chromium são detectados automaticamente — troque com --browser. Ele abre uma aba normal no navegador que você já usa, então não há motor para corrigir nem 150 MB extras.",
    "faq.q6": "O Fenestra é um navegador?",
    "faq.a6": "Não. É um servidor que abre uma aba no seu navegador. Não tem barra de endereço própria, nem abas, nem motor de renderização. Essa é a ideia.",
    "faq.q7": "Roda no macOS ou Linux?",
    "faq.a7": "Ainda não. O servidor é portável, mas o lançador do Edge e a integração com o registro são específicos do Windows. Está no roadmap.",

    "footer.tag": "Fenestra · feito para o duplo clique",
    "footer.release": "Release",
    "footer.license": "Licença MIT",
    "footer.security": "Segurança",

    "code.copy": "Copiar"
  };

  var META = {
    en: {
      title: "Fenestra — double-click a local site, it opens in your browser",
      desc: "Fenestra is a tiny local server for Windows that turns any dist/index.html into a working site in your browser. Edge by default; Chrome, Firefox, Brave, Vivaldi, Opera and Chromium supported."
    },
    pt: {
      title: "Fenestra — dois cliques num site local, ele abre no seu navegador",
      desc: "O Fenestra é um servidor local minúsculo para Windows que transforma qualquer dist/index.html num site funcionando no seu navegador. Edge por padrão; Chrome, Firefox, Brave, Vivaldi, Opera e Chromium também."
    }
  };

  var textNodes = Array.prototype.slice.call(document.querySelectorAll("[data-i18n]"));
  var htmlNodes = Array.prototype.slice.call(document.querySelectorAll("[data-i18n-html]"));
  var EN = {};
  var EN_HTML = {};

  textNodes.forEach(function (el) {
    EN[el.getAttribute("data-i18n")] = el.textContent;
  });
  htmlNodes.forEach(function (el) {
    EN_HTML[el.getAttribute("data-i18n-html")] = el.innerHTML;
  });

  function apply(lang) {
    var pt = lang === "pt";

    textNodes.forEach(function (el) {
      var key = el.getAttribute("data-i18n");
      el.textContent = pt && PT[key] != null ? PT[key] : EN[key];
    });

    htmlNodes.forEach(function (el) {
      var key = el.getAttribute("data-i18n-html");
      el.innerHTML = pt && PT[key] != null ? PT[key] : EN_HTML[key];
    });

    document.documentElement.lang = pt ? "pt-BR" : "en";
    document.title = META[lang].title;

    var desc = document.querySelector('meta[name="description"]');
    if (desc) desc.setAttribute("content", META[lang].desc);

    document.querySelectorAll(".lang button").forEach(function (btn) {
      btn.setAttribute("aria-pressed", btn.getAttribute("data-lang") === lang ? "true" : "false");
    });

    try { localStorage.setItem("fenestra-lang", lang); } catch (e) { /* private mode */ }
  }

  var initial = "en";
  try {
    if (localStorage.getItem("fenestra-lang") === "pt") initial = "pt";
  } catch (e) { /* ignore */ }
  if (new URLSearchParams(location.search).get("lang") === "pt") initial = "pt";
  if (new URLSearchParams(location.search).get("lang") === "en") initial = "en";

  apply(initial);

  document.querySelectorAll(".lang button").forEach(function (btn) {
    btn.addEventListener("click", function () {
      apply(btn.getAttribute("data-lang"));
    });
  });

  /* ------------------------------------------------------------ copy */

  function copyText(text) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
      return navigator.clipboard.writeText(text);
    }
    return new Promise(function (resolve, reject) {
      try {
        var area = document.createElement("textarea");
        area.value = text;
        area.setAttribute("readonly", "");
        area.style.position = "fixed";
        area.style.opacity = "0";
        document.body.appendChild(area);
        area.select();
        document.execCommand("copy");
        document.body.removeChild(area);
        resolve();
      } catch (err) {
        reject(err);
      }
    });
  }

  document.querySelectorAll(".copy").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var pre = btn.parentElement.querySelector("pre");
      if (!pre) return;

      var original = btn.textContent;
      copyText(pre.textContent).then(function () {
        btn.textContent = document.documentElement.lang === "pt-BR" ? "Copiado" : "Copied";
        btn.classList.add("done");
      }).catch(function () {
        btn.textContent = document.documentElement.lang === "pt-BR" ? "Falhou" : "Failed";
      }).then(function () {
        setTimeout(function () {
          btn.textContent = original;
          btn.classList.remove("done");
        }, 1600);
      });
    });
  });

  /* ---------------------------------------------------------- reveal */

  var reveals = Array.prototype.slice.call(document.querySelectorAll(".reveal"));

  if ("IntersectionObserver" in window) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add("in");
          io.unobserve(entry.target);
        }
      });
    }, { threshold: 0.12 });

    reveals.forEach(function (el) { io.observe(el); });
  } else {
    reveals.forEach(function (el) { el.classList.add("in"); });
  }
})();
