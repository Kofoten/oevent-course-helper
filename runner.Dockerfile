FROM mcr.microsoft.com/dotnet/sdk:8.0 AS sdk-source
FROM ghcr.io/actions/actions-runner:latest

USER root

COPY --from=sdk-source /usr/share/dotnet /usr/share/dotnet
COPY --from=sdk-source /usr/bin/dotnet /usr/bin/dotnet

RUN ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet

ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="${PATH}:${DOTNET_ROOT}"

USER runner
