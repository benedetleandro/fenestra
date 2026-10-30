# Releases

[English](RELEASING.md) · Português

O Fenestra é alpha. Releases são **marcos**, não cada commit.

## Política

- A `main` sempre compila. As mudanças se acumulam em **Unreleased** no
  [`CHANGELOG.md`](CHANGELOG.md).
- Uma release acontece só quando o mantenedor pede, ou num marco — nunca porque
  uma correção foi mesclada.
- Uma tag por release (`vX.Y.Z`). Nunca uma tag por mudança.
- A tag precisa bater com o `VERSION`; o CI verifica.
- Release de patch existe só para consertar uma release quebrada. Correção normal
  espera o próximo marco.

## Passos

1. Mover as entradas de `Unreleased` para uma seção de versão no `CHANGELOG.md`.
2. Subir o `VERSION` e as strings de versão do site (`docs/`); o job `version` do
   CI falha se divergirem.
3. Commit `Release X.Y.Z`.
4. Tag e push: `git tag vX.Y.Z && git push origin vX.Y.Z`.
5. O workflow de release compila e publica os artefatos.
6. Atualizar o tap do Homebrew com as URLs e o SHA-256 novos.
7. Fechar as issues e o milestone.

## Versionamento

`0.x` enquanto alpha. Mudanças que quebram sobem a minor. Correções entram no
próximo marco, não numa tag nova. `1.0.0` fica reservado para a primeira versão
estável.
