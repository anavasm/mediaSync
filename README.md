# Introducción

Aplicación que descarga todo el contenido de un **bucket** de Amazon S3 en un directorio especificado.

De esta forma el directorio elegido estará sincronizado con el bucket del servidor S3, descargando los nuevos archivos subidos a S3 y eliminando los que ya no existen del directorio local.

*Esta aplicación es un simple ejemplo de como podemos utilizar el SDK de AWS para .NET*

## Requisitos

- Visual Studio
- [AWS SDK para .NET](https://aws.amazon.com/es/sdk-for-net/)

## Utilización

- Clona el repositorio
- Abir y compilar solución con Visual Studio

## Configuración

Se accede a la configuración de la aplicación desde el icono situado en el *system tray* (junto al reloj de windows).

- Download path: directorio donde se descargará todo el contenido del **bucket** indicado.
- Bucket name: nombre del bucket de S3.
- S3 Access Key / S3 Secret Key: credenciales de acceso a tu cuenta de AWS.