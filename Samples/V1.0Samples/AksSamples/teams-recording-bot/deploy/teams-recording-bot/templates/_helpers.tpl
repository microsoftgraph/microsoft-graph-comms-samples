{{/* Default deployment name */}}
{{- define "fullName" -}}
  {{- default .Chart.Name .Values.override.name -}}
{{- end -}}

{{/* Default namespace */}}
{{- define "namespace" -}}
  {{- default (include "fullName" .) .Values.override.namespace -}}
{{- end -}}

{{/* Check replicaCount is less than maxReplicaCount */}}
{{- define "maxCount" -}}
  {{- if lt (int .Values.scale.maxReplicaCount) 1 -}}
    {{- fail "scale.maxReplicaCount cannot be less than 1" -}}
  {{- end -}}
  {{- if gt (int .Values.scale.replicaCount) (int .Values.scale.maxReplicaCount) -}}
    {{- fail "scale.replicaCount cannot be greater than scale.maxReplicaCount" -}}
  {{- else -}}
    {{- printf "%d" (int .Values.scale.maxReplicaCount) -}}
  {{- end -}}
{{- end -}}
