# Web Crawler - Teste Técnico Elaw

## Visão Geral

Este é um projeto desenvolvido como parte do teste técnico da Elaw. Trata-se de um web crawler desenvolvido em C# que extrai proxies de um site e os salva em um arquivo JSON e em um banco de dados SQLite.

## Funcionalidades

- Extrai os proxies das páginas disponíveis em um site.
- Salva os proxies em um arquivo JSON.
- Salva as informações da execução em um banco de dados SQLite.
- Tira uma captura de tela (arquivo HTML) de cada página.
- Implementa multithreading com um máximo de 3 execuções simultâneas.

## Tecnologias Utilizadas

- C#
- Selenium WebDriver
- Chrome Driver
- .NET Core
- SQLite

## Pré-requisitos

Certifique-se de ter o seguinte instalado em sua máquina:

- [.NET Core SDK](https://dotnet.microsoft.com/download)
- [Chrome WebDriver](https://sites.google.com/a/chromium.org/chromedriver/downloads)

## Como Usar

1. Clone este repositório:

    ```
    git clone https://github.com/seu-usuario/web-crawler.git
    ```

2. Navegue até o diretório do projeto:

    ```
    cd web-crawler
    ```

3. Configure o ambiente:

   - Certifique-se de ter o Chrome WebDriver instalado e acessível no PATH do sistema.

4. Execute o projeto:

    ```
    dotnet run
    ```

5. Após a execução, os resultados serão salvos em um arquivo JSON (`proxies.json`) e no banco de dados SQLite (`proxies.db`).

## Contribuições

Contribuições são bem-vindas! Se você encontrar algum problema ou tiver sugestões de melhorias, sinta-se à vontade para abrir uma issue ou enviar um pull request.

## Licença

Este projeto é licenciado sob a [Licença MIT](LICENSE).

---

Este projeto foi desenvolvido como parte do teste técnico da Elaw, demonstrando a capacidade de criar um web crawler eficiente e escalável para extração de dados da web, conforme os requisitos apresentados.
# WebCrawler_ELAW
