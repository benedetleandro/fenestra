(function () {
  "use strict";

  var PT = {
    "nav.how": "Como funciona",
    "nav.compat": "Compatibilidade",
    "nav.plat": "Plataformas",
    "nav.install": "Instalar",
    "nav.macos": "macOS",
    "nav.download": "Baixar",
    "lang.label": "Idioma",
    "nav.faq": "Dúvidas",
    "skip.label": "Pular para o conteúdo",

    "hero.badge": "Windows · Linux · macOS · Edge, Chrome, Firefox",
    "hero.title": "Dois cliques.<br><span class=\"shine\">E roda.</span>",
    "hero.lead": "O Fenestra é um servidor local minúsculo que transforma qualquer <code>dist/index.html</code> num site funcionando no seu navegador. Sem terminal, sem extensão, sem navegador próprio.",
    "hero.download": "Baixar v0.7.0",
    "hero.how": "Como funciona",
    "hero.meta1": "Licença MIT",
    "hero.meta2": "sem SDK .NET",
    "hero.meta3": "um arquivo, sem instalador",
    "hero.macos": "No macOS? Compile você mesmo →",
    "mock.tab": "Meu app",

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
    "compat.case1": "dist/ · build/ · out/",
    "compat.case2": "rotas SPA",
    "compat.case3": "index.html (arquivo único)",
    "compat.case4": "/src/main.tsx",
    "compat.case5": "API · backend",
    "compat.case6": "pages/index.html com ../",

    "plat.kicker": "Plataformas",
    "plat.title": "Uma ideia, três sistemas",
    "plat.lead": "O mesmo trabalho em todo lugar: transformar uma pasta em localhost e abrir no seu navegador. A embalagem muda porque cada sistema tem suas regras.",
    "plat.win.desc": "Um único executável de 54 KB. Sem instalador, sem runtime — o .NET Framework 4.8 já vem no Windows.",
    "plat.win.run": "Baixe o <code>Fenestra.exe</code> e rode, ou torne-o o manipulador de <code>.html</code>.",
    "plat.win.build": "Compile com o <code>build.ps1</code> usando o <code>csc</code> que já vem no Windows.",
    "plat.linux.desc": "Um binário estático, sem runtime, para amd64 e arm64.",
    "plat.linux.run": "<code>chmod +x fenestra-linux-amd64</code> e rode, ou use <code>--install</code> para registrar o manipulador.",
    "plat.linux.build": "Compile com <code>CGO_ENABLED=0 go build .</code> dentro de <code>linux/</code>.",
    "plat.macos.desc": "Sem bundle notarizado: o CLI vem pelo Homebrew, o app do Finder você compila localmente.",
    "plat.macos.run": "<code>cd macos &amp;&amp; ./build.command</code> compila o CLI e o <code>Fenestra.app</code>.",
    "plat.macos.build": "Só Go — sem Xcode, sem conta Apple, sem quarentena depois de um clone.",
    "plat.audit": "Auditar o código →",
    "plat.note": "Toda release também traz o código-fonte completo em zip e tar.gz. Nada aqui é fechado.",
    "plat.win.tag": ".exe",
    "plat.linux.tag": "estático",
    "plat.macos.tag": "build",

    "mac.title": "Compile você mesmo — essa é a ideia",
    "mac.lead": "Não existe binário notarizado. A Apple cobra US$ 99/ano por isso, e um download sem assinatura é bloqueado pelo Gatekeeper. Então você compila localmente, a partir de um código que pode ler.",
    "mac.s1t": "Instale o Go",
    "mac.s1d": "O instalador oficial é assinado pelo Google, então o macOS não mostra aviso. É a única coisa que você instala.",
    "mac.s2t": "Pegue o código",
    "mac.s2d": "git clone, ou baixe o ZIP se você não tem git.",
    "mac.s3t": "Rode o build",
    "mac.s3d": "O script compila o CLI e o Fenestra.app, e re-assina o app depois de editar o bundle.",
    "mac.s4t": "Use pelo Finder",
    "mac.s4d": "Arraste o Fenestra.app para /Applications, botão direito num index.html, Abrir com → Fenestra → Sempre.",
    "mac.note": "Sem Xcode, sem conta Apple, sem US$ 99. Testado num runner macOS real no CI (arm64); no Intel o build é nativo.",
    "mac.readme": "Instruções completas do macOS →",
    "mac.nogit": "Sem git? Baixe o ZIP, extraia e rode <code>bash build.command</code> — o shell lê o script, então o Gatekeeper não entra.",
    "mac.brewTitle": "Homebrew — só o CLI",
    "mac.brewNote": "Instala o CLI pré-compilado para arm64 e Intel. O app do Finder continua precisando do build acima.",

    "install.kicker": "Instalação",
    "install.title": "Um arquivo. Sem instalador.",
    "install.lead": "Escolha o pacote do seu sistema. Todos são um arquivo só — sem instalador, sem runtime.",
    "install.linux": "No Linux existe uma build nativa: um binário estático, sem Wine e sem runtime. Baixe <code>fenestra-linux-amd64</code> na release e rode com <code>chmod +x</code>.",
    "install.macos": "No macOS não existe binário pronto: a notarização da Apple custa US$ 99/ano. A pasta <code>macos/</code> é um kit de montagem — instale o Go e rode um comando.",
    "install.dl": "Windows",
    "install.dlText": "O .exe de 54 KB acima roda direto. Sem instalador, sem runtime.",
    "dl.download": "Baixar →",
    "dl.guide": "Ver o guia →",
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
    "under.lead": "Um arquivo C# para Windows e dois ports pequenos em Go para Linux e macOS. Sem dependências, compilados por compiladores que você já tem — pequeno o bastante para auditar numa tarde.",
    "under.p1b": "Servidor",
    "under.p1s": "HTTP/1.1 num TcpListener, ligado só a 127.0.0.1. Só headers Host de loopback são aceitos.",
    "under.p2b": "Caminhos",
    "under.p2s": "Normalizados e confinados à raiz servida. .., : e ::$DATA são rejeitados, e symlinks que escapam dela também.",
    "name.kicker": "O nome",
    "name.title": "Uma janela, não um navegador",
    "name.lead": "<em>Fenestra</em> é latim para janela: a abertura, não o cômodo. É a ideia inteira — o Fenestra não renderiza o seu site, ele abre uma janela para ele e deixa o seu navegador olhar através.",
    "name.p2": "A palavra sobrevive no italiano <em>finestra</em>, no francês <em>fenêtre</em> e no português <em>fenestra</em> (a anatômica), então soa como \"janela\" para quem fala uma língua românica e como nome próprio para o resto. É também uma piada discreta: o navegador é a moldura; isto é a abertura.",
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
    "faq.a2": "Não. O Fenestra é um executável de 54 KB compilado contra o .NET Framework que já vem no Windows. Nada para instalar, nada para manter atualizado.",
    "faq.q3": "Por que o Windows pede confirmação para o .html?",
    "faq.a3": "Porque o Windows permite exatamente um programa por extensão e não deixa um app reivindicar isso em silêncio. É proteção contra justamente o tipo de software que sequestra associações de arquivo.",
    "faq.q4": "Ele altera meus arquivos?",
    "faq.a4": "Não. Ele abre em modo leitura, serve e não escreve nada no projeto. O único estado que guarda é um número de porta por pasta, em %LOCALAPPDATA%.",
    "faq.q5": "Quais navegadores ele suporta?",
    "faq.a5": "O navegador do sistema é o padrão. Chrome, Firefox, Brave, Vivaldi, Opera e Chromium são detectados automaticamente — e o Safari no macOS. Use --browser edge para fixar o Edge. Ele abre uma aba normal no navegador que você já usa, então não há motor para corrigir nem 150 MB extras.",
    "faq.q6": "O Fenestra é um navegador?",
    "faq.a6": "Não. É um servidor que abre uma aba no seu navegador. Não tem barra de endereço própria, nem abas, nem motor de renderização. Essa é a ideia.",
    "faq.q7": "Roda no macOS ou Linux?",
    "faq.a7": "Sim. No Linux há binários estáticos (amd64 e arm64) com instalação via --install. No macOS não há binário pronto: o kit em macos/ compila na sua máquina, sem conta Apple e sem os US$ 99/ano da notarização.",

    "nav.browser": "Navegador",
    "browser.settings": "Dê duplo clique no Fenestra sem arquivo para abrir esta tela de novo.",
    "browser.first": "Na primeira vez, o Fenestra pergunta qual navegador usar e salva a resposta.",
    "browser.kicker": "Navegador",
    "browser.title": "Escolha o navegador, uma vez",
    "browser.lead": "O Fenestra abre uma aba normal no navegador que você escolher. A escolha fica salva e vale para todos os próximos duplo cliques.",
    "browser.choose": "Escolher",
    "browser.list": "Veja o que está instalado com --browsers.",
    "browser.system": "Usar o padrão do sistema",
    "browser.systemText": "Com --browser default, quem decide é o sistema. Para mudar o que isso significa:",
    "browser.win": "Windows: Configurações → Aplicativos → Aplicativos padrão → escolha o navegador.",
    "browser.mac": "macOS: Ajustes do Sistema → Área de Trabalho e Dock → Navegador padrão.",
    "browser.linux": "Linux: xdg-settings set default-web-browser firefox.desktop",
    "browser.store": "A escolha fica em %LOCALAPPDATA%\\Fenestra\\browser.txt (Windows), ~/.config/fenestra/browser (Linux) e ~/Library/Application Support/Fenestra/browser (macOS).",

    "footer.tag": "Fenestra · feito para o duplo clique",
    "footer.release": "Release",
    "footer.license": "Licença MIT",
    "footer.security": "Segurança",
    "footer.top": "Voltar ao topo ↑",

    "code.copy": "Copiar"
  };

  var META = {
    en: {
      title: "Fenestra — double-click a local site, it opens in your browser",
      desc: "Fenestra is a tiny local server for Windows, Linux and macOS that turns any dist/index.html into a working site in your browser. Edge by default; Chrome, Firefox, Brave, Vivaldi, Opera and Chromium supported."
    },
    pt: {
      title: "Fenestra — dois cliques num site local, ele abre no seu navegador",
      desc: "O Fenestra é um servidor local minúsculo para Windows, Linux e macOS que transforma qualquer dist/index.html num site funcionando no seu navegador. Edge por padrão; Chrome, Firefox, Brave, Vivaldi, Opera e Chromium também."
    }
  };

  var textNodes = Array.prototype.slice.call(document.querySelectorAll("[data-i18n]"));
  var htmlNodes = Array.prototype.slice.call(document.querySelectorAll("[data-i18n-html]"));
  var ariaNodes = Array.prototype.slice.call(document.querySelectorAll("[data-i18n-aria]"));
  var EN = {};
  var EN_HTML = {};
  var EN_ARIA = {};

  textNodes.forEach(function (el) {
    EN[el.getAttribute("data-i18n")] = el.textContent;
  });
  htmlNodes.forEach(function (el) {
    EN_HTML[el.getAttribute("data-i18n-html")] = el.innerHTML;
  });
  ariaNodes.forEach(function (el) {
    EN_ARIA[el.getAttribute("data-i18n-aria")] = el.getAttribute("aria-label");
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

    ariaNodes.forEach(function (el) {
      var key = el.getAttribute("data-i18n-aria");
      el.setAttribute("aria-label", pt && PT[key] != null ? PT[key] : EN_ARIA[key]);
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

  /* --------------------------------------------------------- progress */

  var progress = document.getElementById("progress");

  function paintProgress() {
    var doc = document.documentElement;
    var max = doc.scrollHeight - doc.clientHeight;
    var ratio = max > 0 ? doc.scrollTop / max : 0;
    if (progress) progress.style.width = (ratio * 100).toFixed(2) + "%";
  }

  window.addEventListener("scroll", paintProgress, { passive: true });
  window.addEventListener("resize", paintProgress);
  paintProgress();

  /* ---------------------------------------------------------- reveal */

  [".split", ".table-wrap", ".platforms", ".points", ".faq-list", ".two-col"].forEach(function (sel) {
    document.querySelectorAll(sel).forEach(function (el) {
      el.classList.add("reveal");
    });
  });

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
