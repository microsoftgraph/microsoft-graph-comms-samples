{{/* Default deployment name */}}
{{- define "fullName" -}}
  {{- default $.Release.Name $.Values.global.override.name -}}
{{- end -}}

{{/* Nginx fullName */}}
{{/*We have to differentiate the context this is called in from sub chart or from parent chart*/}}
{{- define "ingress-nginx.fullname" -}}
  {{- if $.Values.controller -}}
    {{- if $.Values.fullnameOverride -}}
      {{- $.Values.fullnameOverride  | trunc 63 | trimSuffix "-" -}}
    {{- else -}}
      {{- default (printf "%s-ingress-nginx" (include "fullName" .)) $.Values.nameOverride -}}
    {{- end -}}
  {{- else -}}
    {{- if (index $.Values "ingress-nginx" "fullnameOverride") -}}
      {{- (index $.Values "ingress-nginx" "fullnameOverride") -}}
    {{- else -}}
      {{- default (printf "%s-ingress-nginx" (include "fullName" .)) (index $.Values "ingress-nginx" "nameOverride") -}}
    {{- end -}}
  {{- end -}}
{{- end -}}

{{- define "ingress-nginx.instance" -}}
  {{- default $.Release.Name (index $.Values "ingress-nginx" "instance") -}}
{{- end -}}

{{- define "ingress-nginx.name" -}}
  {{- if $.Values.controller -}}
    {{- default (include "ingress-nginx.fullname" .) .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
  {{- else -}}
    {{- default (include "ingress-nginx.fullname" .) (index $.Values "ingress-nginx" "nameOverride") | trunc 63 | trimSuffix "-" -}}
  {{- end -}}
{{- end -}}

{{/*We have to differentiate the context this is called in from sub chart or from parent chart*/}}
{{- define "ingress-nginx.controller.fullname" -}}
  {{- if $.Values.controller -}}
    {{- printf "%s-%s" (include "ingress-nginx.fullname" .) $.Values.controller.name | trunc 63 | trimSuffix "-" -}}
  {{- else -}}
    {{- printf "%s-%s" (include "ingress-nginx.fullname" .) (index $.Values "ingress-nginx" "controller" "name") | trunc 63 | trimSuffix "-" -}}
  {{- end -}}
{{- end -}}

{{/* Default namespace */}}
{{- define "namespace" -}}
  {{- default $.Release.Namespace $.Values.global.override.namespace -}}
{{- end -}}

{{/* Nginx namespace */}}
{{/*We have to differentiate the context this is called in from sub chart or from parent chart*/}}
{{- define "ingress-nginx.namespace" -}}
  {{- if $.Values.controller -}}
    {{- default (include "namespace" .) $.Values.namespaceOverride -}}
  {{- else -}}
    {{- default (include "namespace" .) (index $.Values "ingress-nginx" "namespaceOverride") -}}
  {{- end -}}
{{- end -}}

{{/* Check replicaCount is less than maxReplicaCount */}}
{{- define "maxCount" -}}
  {{- if lt (int $.Values.scale.maxReplicaCount) 1 -}}
    {{- fail "scale.maxReplicaCount cannot be less than 1" -}}
  {{- end -}}
  {{- if gt (int $.Values.scale.replicaCount) (int .Values.scale.maxReplicaCount) -}}
    {{- fail "scale.replicaCount cannot be greater than scale.maxReplicaCount" -}}
  {{- else -}}
    {{- printf "%d" (int $.Values.scale.maxReplicaCount) -}}
  {{- end -}}
{{- end -}}

{{/* Check if issuer email is set */}}
{{- define "cluster-issuer.email" -}}
  {{- if eq $.Values.ingress.tls.email "YOUR_EMAIL" -}}
    {{- fail "You need to specify a ingress tls email for lets encrypt" -}}
  {{- else if $.Values.ingress.tls.email  -}}
    {{- printf "%s" $.Values.ingress.tls.email -}}
  {{- else -}}
    {{- fail "You need to specify a ingress tls email for lets encrypt" -}}
  {{- end -}}
{{- end -}}

{{/*Define ingress-tls secret name*/}}
{{- define "ingress.tls.secretName" -}}
  {{- default (printf "ingress-tls-%s" (include "fullName" .)) $.Values.ingress.tls.secretName -}}    
{{- end -}}

{{/*Define ingress path*/}}
{{- define "ingress.path" -}}
  {{- printf "/%s" (trimPrefix "/" $.Values.ingress.path) -}}    
{{- end -}}

{{/*Define ingress path*/}}
{{- define "ingress.path.withTrailingSlash" -}}
  {{- printf "%s/" (trimSuffix "/" (include "ingress.path" .)) -}}    
{{- end -}}


{{/* Check if host is set */}}
{{- define "hostName" -}}
  {{- if .Values.host -}}
    {{- printf "%s" $.Values.host -}}
  {{- else -}}
    {{- fail "You need to specify a host" -}}
  {{- end -}}
{{- end -}}

{{/* Check if image.domain is set */}}
{{- define "imageDomain" -}}
  {{- if $.Values.image.domain -}}
    {{- printf "%s" $.Values.image.domain -}}
  {{- else -}}
    {{- fail "You need to specify image.domain" -}}
  {{- end -}}
{{- end -}}

{{/* Check if public.ip is set */}}
{{- define "publicIP" -}}
  {{- if $.Values.public.ip -}}
    {{- printf "%s" $.Values.public.ip -}}
  {{- else -}}
    {{- fail "You need to specify public.ip" -}}
  {{- end -}}
{{- end -}}

{{/*Update nginx params with generated tcp-config-map*/}}
{{/*because it is called in the context of the subchart we can only use global values or the subcharts values*/}}
{{- define "ingress-nginx.params" -}}
- /nginx-ingress-controller
- --election-id={{ include "ingress-nginx.controller.electionID" . }}
- --controller-class=k8s.io/{{ include "ingress-nginx.fullname" .}}
- --ingress-class={{ include "ingress-nginx.fullname" .}}
- --configmap=$(POD_NAMESPACE)/{{ include "ingress-nginx.controller.fullname" . }}
- --tcp-services-configmap={{ include "ingress-nginx.namespace" . }}/{{ include "fullName" . }}-tcp-services
{{- end -}}