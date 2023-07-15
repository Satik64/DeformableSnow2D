using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace DeformableSnow2D
{

    public class DeformableSnow2D : MonoBehaviour
    {
        public float regenerationSpeed = 0f; //speed of resnowing or melting if below zero
        public List<Transform> currentCollidersWithSnow; //objects interacting with the snow
        public bool interactWithAllCollidersInScene = false; //disabled by default due to optimization

        private List<PolygonCollider2D> colliders_polygon;
        private List<BoxCollider2D> colliders_boxCollider;
        private List<CircleCollider2D> colliders_circle;

        //snow mesh
        private Mesh deformingMesh;

        //low vertex position for deforming
        private float lowestVertex;

        private List<float> regenerationSpeedRandomizer;

        //List for storing deformable vertices (Red color channel, vertex position in array ,currentDeformation)
        private List<Vector4> verticesInformations;
        private List<float> previousVerticesPosition;
        private List<float> originalVerticesPosition;
        public Object snowObject;

        // Start is called before the first frame update
        void Start()
        {
            //get assigned mesh
            deformingMesh = GetComponent<MeshFilter>().mesh;

            //get lowest vertex
            lowestVertex = GetLowestVertex();

            //prepare list for searching deformable vertices
            verticesInformations = new List<Vector4>();
            previousVerticesPosition = new List<float>();
            originalVerticesPosition = new List<float>();
            regenerationSpeedRandomizer = new List<float>();


            //find all vertices with vertex color bigger than 0 (deformable vertices)
            for (int i = 0; i < deformingMesh.vertexCount; i++)
            {
                if (deformingMesh.colors[i].r > 0.99f)
                {
                    Vector4 prepareVector = new Vector4(deformingMesh.colors[i].r,
                        i,
                        transform.TransformPoint(deformingMesh.vertices[i]).y,
                        transform.TransformPoint(deformingMesh.vertices[i]).x);
                    verticesInformations.Add(prepareVector);
                    previousVerticesPosition.Add(prepareVector.z);

                    originalVerticesPosition.Add(prepareVector.w);

                    regenerationSpeedRandomizer.Add(Random.Range(0.7f, 1.3f));
                }
            }

            //sort result vertices by red color channel (for smooth deformating)
            verticesInformations.OrderBy(v => v.x).ToList();

            colliders_boxCollider = new List<BoxCollider2D>();
            colliders_circle = new List<CircleCollider2D>();
            colliders_polygon = new List<PolygonCollider2D>();

            //find all 2D colliders in the scene if allowed
            if (interactWithAllCollidersInScene)
            {
                FindAllCollidersInScene();
            }
            else
            {
                SortAssignedColliders();
            }
        }

        /// <summary>
        /// foreach assigned objects and recognize the right collision component
        /// </summary>
        private void SortAssignedColliders()
        {
            foreach (Transform coll in currentCollidersWithSnow)
            {
                if (coll.TryGetComponent<BoxCollider2D>(out BoxCollider2D bc))
                {
                    colliders_boxCollider.Add(bc);
                }
                if (coll.TryGetComponent<CircleCollider2D>(out CircleCollider2D cc))
                {
                    colliders_circle.Add(cc);
                }
                if (coll.TryGetComponent<PolygonCollider2D>(out PolygonCollider2D pc))
                {
                    colliders_polygon.Add(pc);
                }
            }
        }

        /// <summary>
        /// Get lowest vertex in the mesh
        /// </summary>
        private float GetLowestVertex()
        {
            float lowestVerticle = transform.TransformPoint(deformingMesh.vertices[0]).y;
            foreach (Vector3 verticle in deformingMesh.vertices)
            {
                if (transform.TransformPoint(verticle).y < lowestVerticle) lowestVerticle = transform.TransformPoint(verticle).y;
            }
            return lowestVerticle;
        }



        private void FindAllCollidersInScene()
        {
            colliders_boxCollider.AddRange((BoxCollider2D[])GameObject.FindObjectsOfType(typeof(BoxCollider2D)));
            colliders_polygon.AddRange((PolygonCollider2D[])GameObject.FindObjectsOfType(typeof(PolygonCollider2D)));
            colliders_circle.AddRange((CircleCollider2D[])GameObject.FindObjectsOfType(typeof(CircleCollider2D)));
        }


        /// <summary>
        /// Try to find any type of collider and get lowest points to calculate snow transition
        /// </summary>
        private Vector4 GetBoxColliderPoints(BoxCollider2D bc)
        {
            if (bc != null)
                return new Vector4(bc.bounds.min.x, bc.bounds.max.x, bc.bounds.min.y, bc.bounds.min.y);
            else return Vector4.one; //return default vector if no collider found
        }



        private void CreateSnowParticle(Vector3 pos)
        {
            if (snowObject != null)
            {
                GameObject dust = Instantiate(snowObject) as GameObject;
                dust.transform.position = pos + Vector3.down * 0.3f;
            }
        }

        /// <summary>
        /// Regenerate snow mesh to the original positions if regenerationSpeed is larger than zero, otherwise melt the snow
        /// </summary>
        private void RegenerateSnow()
        {
            int foreachPosition = 0;
            List<Vector4> newVerticesInformations = verticesInformations;

            foreach (Vector3 verticle in verticesInformations.ToList())
            {
                float verticleVal = verticle.z;
                verticleVal += regenerationSpeed * Time.deltaTime * .1f * regenerationSpeedRandomizer[foreachPosition];
                if (verticleVal < lowestVertex) verticleVal = lowestVertex;
                if (verticleVal > originalVerticesPosition[foreachPosition]) verticleVal = originalVerticesPosition[foreachPosition];

                newVerticesInformations[foreachPosition] =
new Vector4(newVerticesInformations[foreachPosition].x,
newVerticesInformations[foreachPosition].y,
verticleVal, newVerticesInformations[foreachPosition].w);

                foreachPosition += 1;
            }
            verticesInformations = newVerticesInformations;
        }



        public bool IsPointInsideCircle(Vector2 point, CircleCollider2D circleCollider)
        {
            // Převést světové souřadnice na souřadnice relativní k collideru
            point -= (Vector2)circleCollider.transform.position;

            // Vypočítat vzdálenost bodu od středu kruhu
            float distance = Vector2.Distance(point, circleCollider.offset);

            // Porovnat vzdálenost s poloměrem kruhu
            if (distance <= circleCollider.radius * circleCollider.transform.localScale.x)
            {
                // Bod se nachází uvnitř kruhu
                return true;
            }
            else
            {
                // Bod se nachází vně kruhu
                return false;
            }
        }



            public static bool IsPositionInsidePolygon(Vector2 position, PolygonCollider2D polygonCollider)
            {
                int pathCount = polygonCollider.pathCount;
                Matrix4x4 matrix = Matrix4x4.TRS(polygonCollider.transform.position, polygonCollider.transform.rotation, polygonCollider.transform.lossyScale);

                for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
                {
                    Vector2[] colliderPoints = polygonCollider.GetPath(pathIndex);
                    for (int i = 0; i < colliderPoints.Length; i++)
                    {
                        colliderPoints[i] = matrix.MultiplyPoint(colliderPoints[i]);
                    }

                    if (IsPointInPolygon(position, colliderPoints))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
            {
                bool isInside = false;
                int j = polygon.Length - 1;

                for (int i = 0; i < polygon.Length; i++)
                {
                    if ((polygon[i].y < point.y && polygon[j].y >= point.y) || (polygon[j].y < point.y && polygon[i].y >= point.y))
                    {
                        if (polygon[i].x + (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) * (polygon[j].x - polygon[i].x) < point.x)
                        {
                            isInside = !isInside;
                        }
                    }
                    j = i;
                }

                return isInside;
            }
        

        // Update is called once per frame
        void Update()
        {
            //if regeneration speed is set, process the regenerating/melting
            if (regenerationSpeed != 0f)
                RegenerateSnow();

            Vector3[] displacedVertices;
            displacedVertices = deformingMesh.vertices;
            List<Vector4> newVerticesInformations = verticesInformations;

            //setting all vertices in mesh
            int foreachPosition = 0;
            foreach (Vector4 vertexInformation in verticesInformations.ToList())
            {
                //real world position of verticle
                float globalWorldVerticalPosition = vertexInformation.z;
                //float lowestPositionOfCurrentVertex = vertexInformation.z;
                float globalWorldHorizonalPosition = transform.TransformPoint(deformingMesh.vertices[(int)vertexInformation.y]).x;

                // PROCESSING THE COLLIDERS
                foreach (BoxCollider2D collider in colliders_boxCollider)
                {
                    //get length of the collider in the world
                    Vector4 colliderPoints = GetBoxColliderPoints(collider);
                    float colliderLength = colliderPoints.y - colliderPoints.x;

                    //process the deformation if the collider is lower than snow
                    if (colliderPoints.z < globalWorldVerticalPosition)
                    {
                        //remove snow completely if is placed between the collider
                        if (globalWorldHorizonalPosition < (collider.transform.position.x + colliderLength * 0.5f) && globalWorldHorizonalPosition > (collider.transform.position.x - colliderLength * 0.5f))
                        {
                            if (colliderPoints.z < newVerticesInformations[foreachPosition].z)
                            {
                                newVerticesInformations[foreachPosition] =
                                new Vector4(newVerticesInformations[foreachPosition].x,
                                newVerticesInformations[foreachPosition].y,
                                colliderPoints.z,
                                newVerticesInformations[foreachPosition].w);

                            }


                            if (newVerticesInformations[foreachPosition].z < lowestVertex)
                            {
                                newVerticesInformations[foreachPosition] =
                                new Vector4(newVerticesInformations[foreachPosition].x,
                                newVerticesInformations[foreachPosition].y,
                                lowestVertex,
                                newVerticesInformations[foreachPosition].w);
                            }
                        }
                    }

                }



                Vector3 worldPoint = transform.TransformPoint(displacedVertices[(int)vertexInformation.y]);
                worldPoint.y = vertexInformation.z;
                displacedVertices[(int)vertexInformation.y] = transform.InverseTransformPoint(worldPoint);

                // PROCESSING THE COLLIDERS
                foreach (PolygonCollider2D collider in colliders_polygon)
                {
                    //process the deformation if the collider is lower than snow
                    if (IsPositionInsidePolygon(new Vector2(worldPoint.x, worldPoint.y), collider))
                    {
                        {
                            newVerticesInformations[foreachPosition] =
                            new Vector4(newVerticesInformations[foreachPosition].x,
                            newVerticesInformations[foreachPosition].y,
                            newVerticesInformations[foreachPosition].z - 5f * Time.deltaTime,
                            newVerticesInformations[foreachPosition].w);

                        }


                        if (newVerticesInformations[foreachPosition].z < lowestVertex)
                        {
                            newVerticesInformations[foreachPosition] =
                            new Vector4(newVerticesInformations[foreachPosition].x,
                            newVerticesInformations[foreachPosition].y,
                            lowestVertex,
                            newVerticesInformations[foreachPosition].w);
                        }
                    }

                }


                // PROCESSING THE COLLIDERS
                foreach (CircleCollider2D collider in colliders_circle)
                {
                    //process the deformation if the collider is lower than snow
                    if (IsPointInsideCircle(new Vector2(worldPoint.x, worldPoint.y), collider))
                    {
                        {
                            newVerticesInformations[foreachPosition] =
                            new Vector4(newVerticesInformations[foreachPosition].x,
                            newVerticesInformations[foreachPosition].y,
                            newVerticesInformations[foreachPosition].z - 5f * Time.deltaTime,
                            newVerticesInformations[foreachPosition].w);

                        }


                        if (newVerticesInformations[foreachPosition].z < lowestVertex)
                        {
                            newVerticesInformations[foreachPosition] =
                            new Vector4(newVerticesInformations[foreachPosition].x,
                            newVerticesInformations[foreachPosition].y,
                            lowestVertex,
                            newVerticesInformations[foreachPosition].w);
                        }
                    }
                }

                //Create particle at verticle position (if not null)
                if (previousVerticesPosition[foreachPosition] > newVerticesInformations[foreachPosition].z && newVerticesInformations[foreachPosition].z > lowestVertex)
                {
                    CreateSnowParticle(worldPoint);
                }

                previousVerticesPosition[foreachPosition] = newVerticesInformations[foreachPosition].z;
                foreachPosition += 1;
            }


            verticesInformations = newVerticesInformations;
            deformingMesh.vertices = displacedVertices;

        }




    }


}
