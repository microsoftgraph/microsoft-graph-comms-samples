{{/* Default deployment name */}}
{{- define "fullName" -}}
  {{- default $.Release.Name $.Values.global.override.name -}}
{{- end -}}

{{/* Default namespace */}}
{{- define "namespace" -}}
  {{- default $.Release.namespace $.Values.global.override.namespace -}}
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
- --controller-class={{ $.Values.controller.ingressClassResource.controllerValue }}
  {{- if $.Values.ingressClass }}
- --ingress-class={{ $.Values.controller.ingressClass }}
  {{- end }}
- --configmap=$(POD_NAMESPACE)/{{ include "ingress-nginx.controller.fullname" . }}
- --tcp-service-configmap={{ include "ingress-nginx.namespace" . }}/{{ include "fullName" . }}-tcp-services
{{- end -}}