FROM scratch

ENV PORT 8001
EXPOSE $PORT

ADD ca-certificates.crt /etc/ssl/certs/
ADD dctrserver /
CMD ["/dctrserver"]
