# Como contribuir

[English](CONTRIBUTING.md) · Português

Obrigado por considerar. É uma ferramenta pequena de propósito — mantenha pequena.

## Build

Requisitos: Windows 10/11 e o .NET Framework 4.x que já vem nele.
Sem SDK, sem Visual Studio, sem internet.

```powershell
./build.ps1
```

O binário sai em `bin/Fenestra.exe`.

## Testes

```powershell
./tests/run-tests.ps1
```

O script não compila nada; rode `build.ps1` antes. Ele sobe o servidor em modo
headless (`FENESTRA_NO_OPEN=1`), faz requisições HTTP reais contra um fixture
temporário e reporta passe/falha com código de saída não-zero.

## Estilo de código

- **Só C# 5.** O compilador é o do .NET Framework 4.8, então sem interpolação,
  sem `?.`, sem membros com corpo de expressão.
- **Zero dependências.** Só a BCL. Se uma mudança precisa de NuGet, precisa de
  discussão antes.
- **Um arquivo por enquanto.** `src/Fenestra.cs` é um arquivo único de propósito;
  dividir está no roadmap, mas não é requisito para PRs.
- Comentários explicam *por que*, não *o que*.

## Pull requests

- Uma coisa por PR.
- Inclua um teste se mexer em HTTP, resolução de caminho ou lógica de porta.
- Atualize o `README.md` se o comportamento mudar.
- Mantenha o diff legível; sem reformatação de carona.

## Boas primeiras issues

- `--root <pasta>` para servir uma pasta diferente da pasta do arquivo.
- Suporte a Windows ARM64.
- Assinatura de código para o binário de release (remove o aviso do SmartScreen).
- Modo portátil (`fenestra.ini` ao lado do exe, sem `%LOCALAPPDATA%`).
- Mais testes: HEAD, ETag, range requests, caminhos UTF-8.
