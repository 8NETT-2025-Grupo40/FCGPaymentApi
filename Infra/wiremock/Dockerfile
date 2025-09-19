FROM wiremock/wiremock:latest

# Copia stubs para o mesmo caminho do --root-dir
COPY . /home/wiremock/

# Porta interna
EXPOSE 5166

# Sobe o WireMock 
# "--verbose" deixa o log detalhado (mostra cada request, matching, latência, etc.)
# "--global-response-templating" habilita Handlebars em TODAS as respostas ({{now}}, {{randomValue}}, {{request.path}}, etc.)
# "--root-dir", "/home/wiremock" diz onde estão seus stubs (precisa ter /mappings e /__files dentro dessa pasta)
# "--port" porta interna do WireMock (a API vai chamar http://<host>:5166)
CMD ["--verbose", "--global-response-templating", "--root-dir", "/home/wiremock", "--port", "5166"]