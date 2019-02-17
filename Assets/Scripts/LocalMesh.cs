using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalMesh {
    public float NORMALIZATION_STRENGTH = 0.8f;
    private Vector3 CENTER = new Vector3(.5f, .5f, 0);

    public Vector3[] vertices;
    public int[] triangles;
    public List<EdgeVector> edges;
    private bool[,] edgeConnections;
    private Vector3 crossMain;
    private Triangle2D motherTriangle;
    public List<int> usedTriangles = new List<int>();
    public List<DynamicObject> objects;
    private Transform go;
    private Mesh mesh3d;
    private Neighbour neighbour;
    public float[] strengths;

    private bool detectOverlappingOnAllTriangles = true;
    private bool detectOverlappingOnAllEdges = true;
    private bool distortMother = false;
    private bool useStrength = true;

    public LocalMesh(
        Transform go,
            Mesh mesh3d,
            Neighbour neighbour,
            float normalizationStrength,
            bool detectOverlappingOnAllTriangles,
            bool detectOverlappingOnAllEdges,
            bool useStrength,
            List<DynamicObject> objects) {
        this.detectOverlappingOnAllTriangles = detectOverlappingOnAllTriangles;
        this.detectOverlappingOnAllEdges = detectOverlappingOnAllEdges;
        this.useStrength = useStrength;
        this.go = go;
        this.mesh3d = mesh3d;
        this.objects = objects;
        this.neighbour = neighbour;
        NORMALIZATION_STRENGTH = normalizationStrength;
        triangles = neighbour.triangles;

        vertices = new Vector3[neighbour.verticles.Length];
        for (int j = 0; j < vertices.Length; j++) {
            vertices[j] = mesh3d.vertices[neighbour.verticles[j]];
        }
        //Vector3 shift = vertices[triangles[0]];
        //// we wanna first point to be in cross of axis XYZ
        //for (int j = 0; j < vertices.Length; j++) {
        //    vertices[j] = vertices[j] - shift;
        //}

        if (neighbour.usedTriangles != null) {
            usedTriangles = neighbour.usedTriangles;
        }

        rotateMesh();

        strengths = new float[triangles.Length / 3];
        for (int j = 0; j < triangles.Length; j += 3) {
            strengths[j / 3] = getStrength(j);
        }

        flattenMesh();
    }

    public void makeEdges() {
        edgeConnections = new bool[triangles.Length, triangles.Length];
        edges = new List<EdgeVector>();

        foreach (int k in usedTriangles) {
            addEdges(k,
                triangles[k + 0],
                triangles[k + 1],
                triangles[k + 2]);
        }

        edges.Sort((x, y) => y.strength.CompareTo(x.strength));

        // filling edge expected length with knowledge of current 3D object
        // (it's length from 3D, not flattened. But in best scenario
        // we want flattened edge to have same length as one from 3D)
        fillEdgeLength();
    }

    // filling list of edges that will be used to normalization of triangles
    // list won't contain for example edges going to main triangle, because we don't wanna to distort him
    public void addEdges(int i, params int[] indexOfNP) {
        // we need to iterate through indexOfNP (3 elements)
        for (int k = 0; k < indexOfNP.Length; k++) {
            // we need to iterate through indexOfNP again
            // but choose all point that are not current k
            for (int j = 0; j < indexOfNP.Length; j++) {
                if (k != j) {
                    // checking if that edge already exist
                    if (edgeAllowed(indexOfNP[j], indexOfNP[k])
                        && !edgeConnections[indexOfNP[j], indexOfNP[k]]) {
                        // not existing, we need to create that edge from j to k
                        // and mark that edge as created
                        // notice that j->k is different than k->j
                        edges.Add(new EdgeVector(indexOfNP[j], indexOfNP[k], strengths[i / 3]));
                        edgeConnections[indexOfNP[j], indexOfNP[k]] = true;

                        // Debug.Log("adding edge " + indexOfNP[j] + " " + indexOfNP[k] + " s" + strengths[i]);
                    }
                }
            }
        }
    }

    private bool edgeAllowed(int from, int to) {
        // keep in mind that indexes 0,1,2 are for mother triangle
        if (distortMother) {
            // we can distort main triangle, so, we can allow any edge
            return true;
        } else if (to >= 3) {
            // with that edge we are pointing out of main triangle - that's allowed
            return true;
            //} else if (from < 3) {
            //    // allowed because both points are in main triangle, 
            //    // (to is also lower than 3)
            //    // we need to do that because main triangle somehow not always is perfect
            //    return true;
        } else {
            // we are pointing in main triangle from outside - forbidden
            return false;
        }
    }

    public void printError() {
        List<float> errors = new List<float>();
        if (edges.Count == 0) {
            Debug.Log("no edges at all");
            return;
        }

        foreach (EdgeVector edge in edges) {
            Vector3 move = vertices[edge.to] - vertices[edge.from];
            errors.Add(Mathf.Abs((move.magnitude - edge.length) / edge.length));
        }
        float max = errors[0], min = errors[0];
        float averange = 0;
        foreach (float error in errors) {
            if (error > max) {
                max = error;
            } else if (error < min) {
                min = error;
            }
            averange += error;
        }
        averange /= errors.Count;
        Debug.Log("edge (" + (edges.Count + (distortMother ? 0 : 3)) + ") error " +
            "min=" + min.ToString("0.000") + ", " +
            "avg=" + averange.ToString("0.000") + ", " +
            "max=" + max.ToString("0.000"));
    }

    private Vector3 getCross(int k) {
        return Vector3.Cross(
            vertices[triangles[k + 1]] - vertices[triangles[k]],
            vertices[triangles[k + 2]] - vertices[triangles[k]]);
    }

    // returns variable [0..1] that is saying how much that triangle 
    // is pararell to triangle with current object
    private float getStrength(int i) {
        if (!useStrength) return 1;
        Vector3 cross = getCross(i);
        float strength = 1 - Vector3.Angle(crossMain, cross) / 180;
        // we need to make ones close to 1 more important 
        return strength * strength;
    }

    private void drawPositionOfTextureObject(DynamicObject obj, Vector3 direction) {
        Vector3 interpolated = obj.barycentric.Interpolate(
            mesh3d.vertices[neighbour.verticles[0]],
            mesh3d.vertices[neighbour.verticles[1]],
            mesh3d.vertices[neighbour.verticles[2]]);
        float magMult = 1 / crossMain.magnitude;
        Debug.DrawRay(Vector3.Scale((go.rotation * interpolated), go.localScale) + go.position,
            go.rotation * crossMain * magMult / 2, Color.blue);
    }

    public void rotateMesh() {
        crossMain = getCross(0);
        foreach (DynamicObject obj in objects) {
            drawPositionOfTextureObject(obj, crossMain);
        }

        Quaternion qAngle = Quaternion.Inverse(Quaternion.LookRotation(crossMain));
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] = qAngle * vertices[i];
        }
        crossMain = getCross(0);
    }

    private bool isNotStraight(Vector3 cross) {
        return crossMain.x > 0.001 || crossMain.y > 0.001
            || crossMain.x < -0.001 || crossMain.y < -0.001;
    }

    public void flattenMesh() {
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i].z = 0;
        }

        motherTriangle = new Triangle2D(vertices[triangles[0]], vertices[triangles[1]], vertices[triangles[2]]);
    }

    public void normalizeFlatMesh(int times) {
        while (times > 0 && separateOverLappingVerticles()) {
            times--;
        }
        for (int i = 0; i < times; i++) {
            if (detectOverlappingOnAllTriangles) {
                separateOverLappingAllFaces();
            }
            if (detectOverlappingOnAllEdges) {
                separateOverLappingAllEdges();
            }
            separateOverLappingMotherFace();
            normalizeFlatMesh();
        }
    }

    public void buildFirstUsedTriangles() {
        // we will iterate through objects on current triangle
        // and list every that contains at least part of any object
        // (after only flattening, without normalization)
        foreach (DynamicObject obj in objects) {
            // TODO remove that sin rotation
            // obj.rotation = Mathf.Sin(Time.realtimeSinceStartup);

            Vector3[] transformedVerticles = getTransformedByObject(obj);

            // going from end to begining, because we wanna to render first element as last
            // over every other element
            // for (int k = 0; k < localMesh.triangles.Length; k += 3) {
            for (int k = triangles.Length - 3; k >= 0; k -= 3) {
                if (usedTriangles.Contains(k)) {
                    continue;
                }
                Triangle3D triangle = new Triangle3D(transformedVerticles[triangles[k + 0]],
                                            transformedVerticles[triangles[k + 1]],
                                            transformedVerticles[triangles[k + 2]]);
                if (triangle.isOnTexture()) {
                    usedTriangles.Add(k);
                }
            }
        }
    }

    public bool checkForOutsiders() {
        List<int> usedTringlesAfterNormalization = new List<int>();

        foreach (DynamicObject obj in objects) {
            Vector3[] transformedVerticles = getTransformedByObject(obj);
            foreach (int k in usedTriangles) {
                Triangle3D triangle = new Triangle3D(transformedVerticles[triangles[k + 0]],
                                            transformedVerticles[triangles[k + 1]],
                                            transformedVerticles[triangles[k + 2]]);
                if (!usedTringlesAfterNormalization.Contains(k) && triangle.isOnTexture()) {
                    // that triangle is on texture, we should keep him
                    usedTringlesAfterNormalization.Add(k);
                } else {
                    // that triangle is outside texture, we may consider removing him
                    // but it can be used in different objects
                }
            }
        }

        bool same = usedTriangles.Count == usedTringlesAfterNormalization.Count;

        usedTriangles = usedTringlesAfterNormalization;

        return same;
    }

    public bool checkForOutsidersAndNotCommons() {
        List<int> usedTringlesAfterNormalization = new List<int>();
        List<int> usedTringlesWithCommon = new List<int>();

        foreach (DynamicObject obj in objects) {
            Vector3[] transformedVerticles = getTransformedByObject(obj);
            foreach (int k in usedTriangles) {
                Triangle3D triangle = new Triangle3D(transformedVerticles[triangles[k + 0]],
                                            transformedVerticles[triangles[k + 1]],
                                            transformedVerticles[triangles[k + 2]]);
                if (!usedTringlesAfterNormalization.Contains(k) && triangle.isOnTexture()) {
                    // that triangle is on texture, we should keep him
                    usedTringlesAfterNormalization.Add(k);
                } else {
                    // that triangle is outside texture, we may consider removing him
                    // but it can be used in different objects
                }
            }
        }

        // looking for triangles that only one point common with any other triangle
        foreach (int k in usedTriangles) {
            int[] indexesK = {
                triangles[k + 0],
                triangles[k + 1],
                triangles[k + 2]
            };
            if (haveCommonPoints(indexesK, k) || k == 0) {
                usedTringlesWithCommon.Add(k);
            }
        }

        bool same = usedTriangles.Count == usedTringlesWithCommon.Count;
        if (!same) {
            Debug.Log("checkForOutsiders " +
                usedTriangles.Count + " " +
                usedTringlesAfterNormalization.Count + " " +
                usedTringlesWithCommon.Count);
        }

        usedTriangles = usedTringlesWithCommon;

        return same;
    }

    private bool haveCommonPoints(int[] indexesK, int k) {
        foreach (int j in usedTriangles) {
            if (k == j) {
                continue;
            }

            int[] indexesJ = {
                    triangles[j + 0],
                    triangles[j + 1],
                    triangles[j + 2]
                };

            int common = commonPoints(indexesK, indexesJ);
            if (common > 1) {
                return true;
            }
        }
        return false;
    }

    private int commonPoints(int[] indexesK, int[] indexesJ) {
        int common = 0;
        for (int k = 0; k < indexesK.Length; k++) {
            for (int j = 0; j < indexesJ.Length; j++) {
                if (indexesK[k] == indexesJ[j]) {
                    common++;
                }
            }
        }
        return common;
    }

    public bool separateOverLappingVerticles() {
        bool separated = false;
        foreach (EdgeVector edge in edges) {
            if (edge.to < 3) {
                // we cannot move main triangle
                continue;
            }

            Vector3 move = vertices[edge.to] - vertices[edge.from];
            if (move.magnitude == 0) {
                moveEndOfEdgeAway(edge);
                separated = true;
            }
        }

        return separated;
    }

    public void separateOverLappingMotherFace() {
        foreach (EdgeVector edge in edges) {
            if (edge.to < 3) {
                // we cannot move main triangle
                continue;
            }
            if (motherTriangle.pointInside(vertices[edge.to])) {
                moveEndOfEdgeAway(edge);
            }
        }
    }

    public void separateOverLappingAllFaces() {
        foreach (int k in usedTriangles) {
            foreach (int j in usedTriangles) {
                if (k == j) {
                    continue;
                }

                Triangle2D triangleK = new Triangle2D(
                    vertices[triangles[k + 0]],
                    vertices[triangles[k + 1]],
                    vertices[triangles[k + 2]]);

                Triangle2D triangleJ = new Triangle2D(
                    vertices[triangles[j + 0]],
                    vertices[triangles[j + 1]],
                    vertices[triangles[j + 2]]);

                if (triangleK.overlaps(triangleJ)) {
                    foreach (EdgeVector edge in edges) {
                        if (edge.to < 3) {
                            // we cannot move main triangle
                            continue;
                        }

                        if (
                               edge.to == triangles[k + 0]
                            || edge.to == triangles[k + 1]
                            || edge.to == triangles[k + 2]) {
                            moveEndOfEdgeAway(edge);
                        }
                    }
                }
            }
        }
    }

    public void separateOverLappingAllEdges() {
        foreach (EdgeVector edge in edges) {
            if (edge.to < 3) {
                // we cannot move main triangle
                continue;
            }

            foreach (int k in usedTriangles) {
                int[] indexes = {
                    triangles[k + 0],
                    triangles[k + 1],
                    triangles[k + 2]};

                if (indexes[0] == edge.to ||
                    indexes[1] == edge.to ||
                    indexes[2] == edge.to) {
                    continue;
                }

                Triangle2D triangle = new Triangle2D(
                    vertices[indexes[0]],
                    vertices[indexes[1]],
                    vertices[indexes[2]]);

                if (triangle.pointInside(vertices[edge.to])) {
                    moveEndOfEdgeAway(edge);
                }
            }
        }
    }

    // vector to outside of mother triangle
    private void moveEndOfEdgeAway(EdgeVector edge) {
        Vector3 move =
             -(vertices[0] - vertices[edge.from]
             + vertices[1] - vertices[edge.from]
             + vertices[2] - vertices[edge.from]);

        // float currentLength = Mathf.Abs(move.magnitude);
        // float wantedLength = currentLength + (edge.length - currentLength) * NORMALIZATION_STRENGTH * edge.strength;
        // vertices[edge.to] = vertices[edge.from] + move * (wantedLength / currentLength);
        vertices[edge.to] = vertices[edge.from] + move * Mathf.Abs(edge.length / move.magnitude);
    }

    public void normalizeFlatMesh() {
        foreach (EdgeVector edge in edges) {
            Vector3 move = vertices[edge.to] - vertices[edge.from];
            float currentLength = Mathf.Abs(move.magnitude);
            float wantedLength = currentLength + (edge.length - currentLength) * NORMALIZATION_STRENGTH * edge.strength;
            vertices[edge.to] = vertices[edge.from] + move * (wantedLength / currentLength);
        }
    }

    public void fillEdgeLength() {
        foreach (EdgeVector edge in edges) {
            edge.length = Mathf.Abs(Vector3.Distance(
                mesh3d.vertices[neighbour.verticles[edge.from]],
                mesh3d.vertices[neighbour.verticles[edge.to]]));

            // Debug.Log("filing edge " + edge.from + " " + edge.to + " l:" + edge.length);
            // edge.length = Mathf.Abs(Vector3.Distance(vertices[edge.from], vertices[edge.to]));
        }
    }

    public void setCenter(Vector3 center) {
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] -= center;
        }
    }

    public Vector3[] getTransformedByObject(DynamicObject obj) {
        // that's interpolated center of ring on planar 3d triangle
        Vector3 interpolated = obj.barycentric.Interpolate(vertices[0], vertices[1], vertices[2]);
        Vector3[] transformedVerticles = new Vector3[vertices.Length];

        for (int k = 0; k < vertices.Length; k++) {
            // moving to center of coords
            Vector3 transformed = vertices[k] - interpolated;
            // scaling 
            transformed *= (1 / obj.scale);
            // rotation
            transformed = rotatePoint(transformed, obj.rotation);
            // setting center as center of bitmap
            transformed += CENTER;

            transformedVerticles[k] = transformed;
        }

        return transformedVerticles;
    }

    private Vector3 rotatePoint(Vector3 pointToRotate, Vector3 centerPoint, float angleInDegrees) {
        float angleInRadians = angleInDegrees * 360 * (Mathf.PI / 180);
        float cosTheta = Mathf.Cos(angleInRadians);
        float sinTheta = Mathf.Sin(angleInRadians);
        return new Vector3(
                (cosTheta * (pointToRotate.x - centerPoint.x) - sinTheta * (pointToRotate.y - centerPoint.y) + centerPoint.x),
                (sinTheta * (pointToRotate.x - centerPoint.x) + cosTheta * (pointToRotate.y - centerPoint.y) + centerPoint.y),
                pointToRotate.z
        );
    }

    private Vector3 rotatePoint(Vector3 pointToRotate, float angleInDegrees) {
        float angleInRadians = angleInDegrees * 360 * (Mathf.PI / 180);
        float cosTheta = Mathf.Cos(angleInRadians);
        float sinTheta = Mathf.Sin(angleInRadians);
        return new Vector3(
                (cosTheta * (pointToRotate.x) - sinTheta * (pointToRotate.y)),
                (sinTheta * (pointToRotate.x) + cosTheta * (pointToRotate.y)),
                pointToRotate.z
        );
    }
}
