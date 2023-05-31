FROM debian:latest

# pre install 
RUN apt-get update
RUN apt-get upgrade -y
RUN apt-get install -y wget

# install dotnet
RUN wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN rm packages-microsoft-prod.deb

RUN  apt-get update && \
    apt-get install -y dotnet-sdk-7.0

RUN apt-get update && \
    apt-get install -y aspnetcore-runtime-7.0

RUN apt-get install -y dotnet-runtime-7.0
RUN dotnet tool install --global dotnet-ef

# expose port
EXPOSE 8080
COPY . /backend
WORKDIR /backend

CMD ["dotnet", "run"]